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
using WebMason_final.Server.Utils;

namespace WebMason_final.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordpressController : ControllerBase
    {
        private readonly DockerClient _dockerClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WordpressController> _logger;
        private readonly EmailService _emailService;

        public WordpressController(ApplicationDbContext context, EmailService emailService)
        {
            _dockerClient = new DockerClientConfiguration(new Uri("http://[2a03:5840:111:1025:44:f6ff:fe38:b5a1]:2375")).CreateClient();
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployWordpress([FromBody] WordpressDeployModel model)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                
                model.UserId = userId.ToString();

                string netname = "network-" + model.UserId;
                string baseContnameMySQL = "mysql-container-" + model.UserId;
                string baseContnameWordpress = "wordpress-container-" + model.UserId;
                string contnameMySQL = baseContnameMySQL;
                string contnameWordpress = baseContnameWordpress;

                
                var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters());
                var network = networks.FirstOrDefault(n => n.Name == netname);

                
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

                
                await PullImageIfNotExists("mysql:5.7");
                await PullImageIfNotExists("wordpress:latest");

                
                int availableMySQLPort = GetAvailablePort();
                int availableWordPressPort = GetAvailablePort();

                
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contnameMySQL)))
                {
                    contnameMySQL = $"{baseContnameMySQL}-{counter}";
                    counter++;
                }

                
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

                
                var mysqlContainerInfo = await _dockerClient.Containers.InspectContainerAsync(mysqlContainer.ID);
                var mysqlHostname = mysqlContainerInfo.Config.Hostname;

                
                containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

                
                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameWordpress)))
                {
                    contnameWordpress = $"{baseContnameWordpress}-{counter}";
                    counter++;
                }

                
                var wordpressContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "wordpress:latest",
                    Name = contnameWordpress,
                    Env = new List<string>
                    {
                        $"WORDPRESS_DB_HOST={mysqlHostname}",
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

                
                var wordpressContainerInfo = await _dockerClient.Containers.InspectContainerAsync(wordpressContainer.ID);
                var wordpressHostname = wordpressContainerInfo.Config.Hostname;

                
                var serverOrder = new ServerOrder
                {
                    Id = Guid.NewGuid(),
                    ServerType = "WordPress",
                    OrderDate = DateTime.UtcNow,
                    UserId = userId,
                    User = user,
                    MySQLContainerId = mysqlHostname,
                    WordPressContainerId = wordpressHostname,
                    MySQLPort = availableMySQLPort,
                    WordPressPort = availableWordPressPort,
                    OdooContainerId = "null",
                    OdooPostgreSQLContainerId = "null",
                    OdooPostgreSQLPort = 0,
                    OdooPort = 0,
                    RedmineContainerId = "null",
                    RedmineMySQLContainerId = "null",
                    RedmineMySQLPort = 0,
                    RedminePort = 0
                };
                _context.ServerOrders.Add(serverOrder);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(user.Email, "Votre service Wordpress", $"Votre instance Wordpress a été déployée avec succès à l'url suivant : http://webmason.fr:{availableWordPressPort} .  " +
                    $"Profitez bien de vos nouveaux services. \r\n Pour toute question veuillez contacter le service technique. \r\n Mathis Bureau.\r\n Lead developer");

                return Ok(new { Message = $"WordPress déployé avec succès sur le port : {availableWordPressPort}", WordPressPort = availableWordPressPort, MySQLPort = availableMySQLPort });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du déploiement de conteneurs", error = ex.Message });
            }
        }

        [HttpPost("deploy-odoo")]
        public async Task<IActionResult> DeployOdoo([FromBody] OdooDeployModel model)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                
                model.UserId = userId.ToString();

                string netname = "network-" + model.UserId;
                string baseContnamePostgres = "postgres-container-" + model.UserId;
                string baseContnameOdoo = "odoo-container-" + model.UserId;
                string contnamePostgres = baseContnamePostgres;
                string contnameOdoo = baseContnameOdoo;

                
                var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters());
                var network = networks.FirstOrDefault(n => n.Name == netname);

                
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

                
                await PullImageIfNotExists("postgres:latest");
                await PullImageIfNotExists("odoo:latest");

                
                int availablePostgresPort = GetAvailablePort();
                int availableOdooPort = GetAvailablePort();

                
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contnamePostgres)))
                {
                    contnamePostgres = $"{baseContnamePostgres}-{counter}";
                    counter++;
                }

                
                var postgresContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "postgres:latest",
                    Name = contnamePostgres,
                    Env = new List<string>
                    {
                        $"POSTGRES_PASSWORD={model.PostgresPassword}",
                        $"POSTGRES_DB=postgres",
                        $"POSTGRES_USER={model.PostgresUser}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "5432/tcp", new List<PortBinding> { new PortBinding { HostPort = availablePostgresPort.ToString() } } }
                        }
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(postgresContainer.ID, new ContainerStartParameters());

                
                containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

                
                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameOdoo)))
                {
                    contnameOdoo = $"{baseContnameOdoo}-{counter}";
                    counter++;
                }

                
                var odooContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "odoo:latest",
                    Name = contnameOdoo,
                    Env = new List<string>
                    {
                        $"HOST={contnamePostgres}",
                        $"PORT=5432",
                        $"POSTGRES_DB=postgres",
                        $"POSTGRES_USER={model.PostgresUser}",
                        $"POSTGRES_PASSWORD={model.PostgresPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "8069/tcp", new List<PortBinding> { new PortBinding { HostPort = availableOdooPort.ToString() } } }
                        }
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(odooContainer.ID, new ContainerStartParameters());

                
                var odooContainerInfo = await _dockerClient.Containers.InspectContainerAsync(odooContainer.ID);
                var odooHostname = odooContainerInfo.Config.Hostname;

                
                var postgreContainerInfo = await _dockerClient.Containers.InspectContainerAsync(postgresContainer.ID);
                var postgreHostname = postgreContainerInfo.Config.Hostname;


                
                var serverOrder = new ServerOrder
                {
                    Id = Guid.NewGuid(),
                    ServerType = "Odoo",
                    OrderDate = DateTime.UtcNow,
                    UserId = userId,
                    User = user,
                    OdooPostgreSQLContainerId = postgreHostname,
                    OdooPostgreSQLPort = availablePostgresPort,
                    OdooContainerId = odooHostname,
                    OdooPort = availableOdooPort,
                    MySQLContainerId = "null",
                    WordPressContainerId = "null",
                    MySQLPort = 0,
                    WordPressPort = 0,
                    RedmineContainerId = "null",
                    RedmineMySQLContainerId = "null",
                    RedmineMySQLPort = 0,
                    RedminePort = 0
                };
                _context.ServerOrders.Add(serverOrder);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(user.Email, "Votre service Odoo", $"Votre instance Odoo a été déployée avec succès à l'url suivant : http://webmason.fr:{availableOdooPort} .  " +
                    $"Profitez bien de vos nouveaux services. \r\n Pour toute question veuillez contacter le service technique. \r\n Mathis Bureau.\r\n Lead developer");

                return Ok(new { Message = $"Odoo déployé avec succès sur le port : {availableOdooPort}", OdooPort = availableOdooPort, PostgresPort = availablePostgresPort });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du déploiement de conteneurs", error = ex.Message });
            }
        }


        [HttpPost("deploy-redmine")]
        public async Task<IActionResult> DeployRedmine([FromBody] RedmineDeployModel model)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                model.UserId = userId.ToString();

                string netname = "network-redmine-" + model.UserId;
                string baseContnameMySQL = "redmine-mysql-container-" + model.UserId;
                string baseContnameRedmine = "redmine-container-" + model.UserId;
                string contnameMySQL = baseContnameMySQL;
                string contnameRedmine = baseContnameRedmine;

                
                var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters());
                var network = networks.FirstOrDefault(n => n.Name == netname);

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

                
                await PullImageIfNotExists("mysql:5.7");
                await PullImageIfNotExists("redmine:latest");

                
                int availableMySQLPort = GetAvailablePort();
                int availableRedminePort = GetAvailablePort();

                
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contnameMySQL)))
                {
                    contnameMySQL = $"{contnameMySQL}-{counter}";
                    counter++;
                }


                
                var mysqlContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "mysql:5.7",
                    Name = contnameMySQL,
                    Env = new List<string>
                    {
                        $"MYSQL_USER={model.MysqlUser}",
                        $"MYSQL_PASSWORD={model.MysqlPassword}",
                        $"MYSQL_DATABASE={model.MysqlDatabase}",
                        $"MYSQL_ROOT_PASSWORD={model.MysqlRootPassword}"
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

                
                await Task.Delay(5000);

                
                var mysqlContainerInfo = await _dockerClient.Containers.InspectContainerAsync(mysqlContainer.ID);
                var mysqlHostname = mysqlContainerInfo.Config.Hostname;

                
                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameRedmine)))
                {
                    contnameRedmine = $"{contnameRedmine}-{counter}";
                    counter++;
                }

                await Task.Delay(10000);

                
                var redmineContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "redmine:latest",
                    Name = contnameRedmine,
                    Env = new List<string>
                    {
                        $"REDMINE_DB_MYSQL={mysqlHostname}",
                        $"REDMINE_DB_USERNAME={model.MysqlUser}",
                        $"REDMINE_DB_PASSWORD={model.MysqlPassword}"
                    },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = netname,
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "3000/tcp", new List<PortBinding> { new PortBinding { HostPort = availableRedminePort.ToString() } } }
                        }
                    }
                });

                await _dockerClient.Containers.StartContainerAsync(redmineContainer.ID, new ContainerStartParameters());

                
                var redmineContainerInfo = await _dockerClient.Containers.InspectContainerAsync(redmineContainer.ID);
                var redmineHostname = redmineContainerInfo.Config.Hostname;

                var serverOrder = new ServerOrder
                {
                    Id = Guid.NewGuid(),
                    ServerType = "Redmine",
                    OrderDate = DateTime.UtcNow,
                    UserId = userId,
                    User = user,
                    RedmineMySQLContainerId = mysqlHostname,
                    RedmineContainerId = redmineHostname,
                    RedmineMySQLPort = availableMySQLPort,
                    RedminePort = availableRedminePort,
                    MySQLContainerId = "null",
                    WordPressContainerId = "null",
                    MySQLPort = 0,
                    WordPressPort = 0,
                    OdooContainerId = "null",
                    OdooPostgreSQLContainerId = "null",
                    OdooPostgreSQLPort = 0,
                    OdooPort = 0
                };
                _context.ServerOrders.Add(serverOrder);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(user.Email, "Votre service Redmine", $"Votre instance Redmine a été déployée avec succès à l'url suivant : http://webmason.fr:{availableRedminePort} .  " +
                    $"Profitez bien de vos nouveaux services. \r\n Pour toute question veuillez contacter le service technique. \r\n Mathis Bureau.\r\n Lead developer");

                return Ok(new { Message = $"Redmine déployé avec succès sur le port : {availableRedminePort}", RedminePort = availableRedminePort, MySQLPort = availableMySQLPort });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du déploiement de conteneurs", error = ex.Message });
            }
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllContainers()
        {
            try
            {
                // a utiliser pour supprimer tous les conteneurs de la base de données
                _context.RemoveRange(_context.ServerOrders);

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

        [HttpDelete("delete/{serverOrderId}")]
        public async Task<IActionResult> DeleteContainer(Guid serverOrderId)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                var serverOrder = await _context.ServerOrders
                    .FirstOrDefaultAsync(so => so.Id == serverOrderId && so.UserId == userId);

                if (serverOrder == null)
                {
                    return NotFound();
                }

                string netname = "network-" + userId.ToString();

                
                _context.ServerOrders.Remove(serverOrder);
                await _context.SaveChangesAsync();

                
                if (serverOrder.ServerType == "WordPress")
                {
                    await DeleteContainersAndResources(serverOrder.WordPressContainerId, serverOrder.MySQLContainerId, netname);
                    await _emailService.SendEmailAsync(user.Email, "Suppression de votre service Wordpress", $"Votre instance Wordpress a été supprimée avec succès.\r\n" +
                    $"Nous sommes navré si le service ne vous a pas satisfait et espérons vous revoir bientôt. \r\n Pour toute question supplémentaire veuillez contacter le service technique. \r\n \r\n \r\nMathis Bureau.\r\n Lead developer");
                }
                else if (serverOrder.ServerType == "Odoo")
                {
                    await DeleteContainersAndResources(serverOrder.OdooContainerId, serverOrder.OdooPostgreSQLContainerId, netname);
                    await _emailService.SendEmailAsync(user.Email, "Suppression de votre service Odoo", $"Votre instance Odoo a été supprimée avec succès.\r\n" +
                    $"Nous sommes navré si le service ne vous a pas satisfait et espérons vous revoir bientôt. \r\n Pour toute question supplémentaire veuillez contacter le service technique. \r\n \r\n \r\n Mathis Bureau.\r\n Lead developer");
                }
                else if (serverOrder.ServerType == "Redmine")
                {
                    await DeleteContainersAndResources(serverOrder.RedmineContainerId, serverOrder.RedmineMySQLContainerId, netname);
                    await _emailService.SendEmailAsync(user.Email, "Suppression de votre service Redmine", $"Votre instance Redmine a été supprimée avec succès.\r\n" +
                    $"Nous sommes navré si le service ne vous a pas satisfait et espérons vous revoir bientôt. \r\n Pour toute question supplémentaire veuillez contacter le service technique. \r\n \r\n \r\n Mathis Bureau.\r\n Lead developer");
                }

                return Ok(new { Message = "Container deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting container", error = ex.Message });
            }
        }

        private async Task DeleteContainersAndResources(string mainContainerId, string dbContainerId, string networkName)
        {
            // Arrêter et supprimer le conteneur principal (WordPress, Odoo, Redmine)
            if (!string.IsNullOrEmpty(mainContainerId))
            {
                await _dockerClient.Containers.StopContainerAsync(mainContainerId, new ContainerStopParameters());
                await _dockerClient.Containers.RemoveContainerAsync(mainContainerId, new ContainerRemoveParameters { Force = true });
            }

            // Arrêter et supprimer le conteneur de base de données (MySQL, PostgreSQL)
            if (!string.IsNullOrEmpty(dbContainerId))
            {
                await _dockerClient.Containers.StopContainerAsync(dbContainerId, new ContainerStopParameters());
                await _dockerClient.Containers.RemoveContainerAsync(dbContainerId, new ContainerRemoveParameters { Force = true });
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

    public class OdooDeployModel
    {
        public string UserId { get; set; }
        public string PostgresUser { get; set; }
        public string PostgresPassword { get; set; }
        public string PostgresDatabase { get; set; }
        public int HostPort { get; set; }
    }


    public class RedmineDeployModel
    {
        public string UserId { get; set; }
        public string MysqlRootPassword { get; set; }
        public string MysqlDatabase { get; set; }
        public string MysqlUser { get; set; }
        public string MysqlPassword { get; set; }
        public int HostPort { get; set; }
    }
}
