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
using System.ComponentModel;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordpressController : ControllerBase
    {
        private readonly DockerClient _dockerClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WordpressController> _logger;

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
                string baseContnameMySQL = "mysql-container-" + model.UserId;
                string baseContnameWordpress = "wordpress-container-" + model.UserId;
                string contnameMySQL = baseContnameMySQL;
                string contnameWordpress = baseContnameWordpress;

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

                    if (!String.IsNullOrEmpty(networkResponse.Warning))
                    {
                        return StatusCode(500, networkResponse.Warning);
                    }
                }

                // Vérifiez et tirez les images Docker si nécessaire
                await PullImageIfNotExists("mysql:5.7");
                await PullImageIfNotExists("wordpress:latest");

                // Trouvez des ports disponibles
                int availableMySQLPort = GetAvailablePort();
                int availableWordPressPort = GetAvailablePort();

                // Vérifiez si le nom du conteneur MySQL existe déjà et ajoutez un numéro si nécessaire
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contnameMySQL)))
                {
                    contnameMySQL = $"{baseContnameMySQL}-{counter}";
                    counter++;
                }

                // Créez et démarrez le conteneur MySQL
                var mysqlContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "mysql:5.7",
                    Name = contnameMySQL,
                    Env = new List<string>
                    {
                        $"MYSQL_ROOT_PASSWORD={model.MysqlRootPassword}",
                        $"MYSQL_DATABASE={model.MysqlDatabase}",
                        $"MYSQL_USER={model.MysqlUser}",
                        $"MYSQL_PASSWORD={model.MysqlPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "3306/tcp", new List<PortBinding> { new PortBinding { HostPort = availableMySQLPort.ToString() } } }
                }
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(mysqlContainer.ID, new ContainerStartParameters());

                // Réinitialisez la liste des conteneurs pour le conteneur WordPress
                containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });


                // Réinitialisez le compteur pour le conteneur WordPress
                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameWordpress)))
                {
                    contnameWordpress = $"{baseContnameWordpress}-{counter}";
                    counter++;
                }

                // Créez et démarrez le conteneur WordPress
                var wordpressContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "wordpress:latest",
                    Name = contnameWordpress,
                    Env = new List<string>
                    {
                        $"WORDPRESS_DB_HOST={contnameMySQL}",
                        $"WORDPRESS_DB_NAME={model.MysqlDatabase}",
                        $"WORDPRESS_DB_USER={model.MysqlUser}",
                        $"WORDPRESS_DB_PASSWORD={model.MysqlPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "80/tcp", new List<PortBinding> { new PortBinding { HostPort = availableWordPressPort.ToString() } } }
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
                    User = user,
                    MySQLContainerId = mysqlContainer.ID,
                    WordPressContainerId = wordpressContainer.ID
                };
                _context.ServerOrders.Add(serverOrder);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "WordPress deployed successfully", WordPressPort = availableWordPressPort, MySQLPort = availableMySQLPort });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deploying WordPress", error = ex.Message });
            }
        }


        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllContainers()
        {
            try
            {
                // a utiliser pour supprimer tous les conteneurs de la base de données
                //_context.RemoveRange(_context.ServerOrders);

                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                // Récupérer l'utilisateur à partir de la base de données
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Récupérer tous les conteneurs appartenant à cet utilisateur
                var userContainers = await _context.ServerOrders
                    .Where(so => so.UserId == userId)
                    .ToListAsync();

                foreach (var container in userContainers)
                {
                    // Arrêter et supprimer le conteneur
                    await _dockerClient.Containers.StopContainerAsync(container.WordPressContainerId, new ContainerStopParameters());
                    await _dockerClient.Containers.RemoveContainerAsync(container.WordPressContainerId, new ContainerRemoveParameters { Force = true });

                    // Arrêter et supprimer le conteneur
                    await _dockerClient.Containers.StopContainerAsync(container.MySQLContainerId, new ContainerStopParameters());
                    await _dockerClient.Containers.RemoveContainerAsync(container.MySQLContainerId, new ContainerRemoveParameters { Force = true });

                    // Supprimer l'entrée de la base de données
                    _context.ServerOrders.Remove(container);
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "All containers deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting containers", error = ex.Message });
            }
        }

        [HttpGet("user-containers")]
        public async Task<IActionResult> GetUserContainers()
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                var userContainers = await _context.ServerOrders
                    .Where(so => so.UserId == userId)
                    .ToListAsync();

                return Ok(userContainers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving containers");
                return StatusCode(500, new { message = "Error retrieving containers", error = ex.Message });
            }
        }

        [HttpGet("delete/{containerId}")]
        public async Task<IActionResult> DeleteContainer(Guid containerId)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                var container = await _context.ServerOrders
                    .FirstOrDefaultAsync(so => so.Id == containerId && so.UserId == userId);

                if (container == null)
                {
                    return NotFound();
                }

                // Arrêter et supprimer les conteneurs associés
                await _dockerClient.Containers.StopContainerAsync(containerId.ToString(), new ContainerStopParameters());
                await _dockerClient.Containers.RemoveContainerAsync(containerId.ToString(), new ContainerRemoveParameters { Force = true });

                _context.ServerOrders.Remove(container);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Container deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting container", error = ex.Message });
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
