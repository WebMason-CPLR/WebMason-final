using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Net;
using Microsoft.EntityFrameworkCore;
using WebMason_final.Server.Models;
using WebMason_final.Server.Data;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordpressController : ControllerBase
    {
        private readonly DockerClient _dockerClient;
        private readonly ApplicationDbContext _context;

        public WordpressController(ApplicationDbContext context)
        {
            _dockerClient = new DockerClientConfiguration(new Uri("http://[2a03:5840:111:1025:44:f6ff:fe38:b5a1]:2375")).CreateClient();
            _context = context;
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployWordpress([FromBody] WordpressDeployModel model)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                // Récupérer l'utilisateur à partir de la base de données
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Assurez-vous que l'ID utilisateur est défini dans le modèle
                model.UserId = userId.ToString();

                string netname = "network-" + model.UserId;
                string baseContname = "wordpress-container-" + model.UserId;
                string contname = baseContname;

                // Vérifiez si le réseau existe déjà
                var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters());
                var network = networks.FirstOrDefault(n => n.Name == netname);

                // Si le réseau n'existe pas, créez-le
                if (network == null)
                {
                    var networkResponse = await _dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = netname,
                        Driver = "bridge"
                    });

                    if (networkResponse.Warning != null)
                    {
                        return StatusCode(500, networkResponse.Warning);
                    }
                }

                // Vérifiez et tirez les images Docker si nécessaire
                await PullImageIfNotExists("mysql:latest");
                await PullImageIfNotExists("wordpress:latest");

                // Trouvez un port disponible
                int availablePort = GetAvailablePort();

                // Vérifiez si le nom du conteneur existe déjà et ajoutez un numéro si nécessaire
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contname)))
                {
                    contname = $"{baseContname}-{counter}";
                    counter++;
                }

                // Créez et démarrez les conteneurs MySQL et WordPress ici

                var mysqlContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "mysql:latest",
                    Name = contname,
                    Env = new List<string>
                    {
                        $"MYSQL_ROOT_PASSWORD={model.MysqlRootPassword}",
                        $"MYSQL_DATABASE={model.MysqlDatabase}",
                        $"MYSQL_USER={model.MysqlUser}",
                        $"MYSQL_PASSWORD={model.MysqlPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(mysqlContainer.ID, new ContainerStartParameters());

                var wordpressContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "wordpress:latest",
                    Name = contname,
                    Env = new List<string>
                    {
                        "WORDPRESS_DB_HOST=mysql-container",
                        $"WORDPRESS_DB_NAME={model.MysqlDatabase}",
                        $"WORDPRESS_DB_USER={model.MysqlUser}",
                        $"WORDPRESS_DB_PASSWORD={model.MysqlPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "80/tcp", new List<PortBinding> { new PortBinding { HostPort = availablePort.ToString() } } }
                        }
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(wordpressContainer.ID, new ContainerStartParameters());

                // Enregistrez une entrée dans la base de données
                var serverOrder = new ServerOrder
                {
                    Id = Guid.NewGuid(),
                    ServerType = "WordPress",
                    OrderDate = DateTime.UtcNow,
                    UserId = userId,
                    User = user
                };
                _context.ServerOrders.Add(serverOrder);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "WordPress deployed successfully", Port = availablePort });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deploying WordPress", error = ex.Message });
            }
        }

        private async Task PullImageIfNotExists(string imageName)
        {
            var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters { All = true });
            if (!images.Any(i => i.RepoTags != null && i.RepoTags.Contains(imageName)))
            {
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = imageName },
                    null,
                    new Progress<JSONMessage>()
                );
            }
        }

        public int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    public class WordpressDeployModel
    {
        public string UserId { get; set; }
        public string MysqlRootPassword { get; set; }
        public string MysqlDatabase { get; set; }
        public string MysqlUser { get; set; }
        public string MysqlPassword { get; set; }
        public int HostPort { get; set; }
    }
}
