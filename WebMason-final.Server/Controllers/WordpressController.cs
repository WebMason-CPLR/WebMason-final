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

                // Récupérez le "hostname" du conteneur MySQL après démarrage
                var mysqlContainerInfo = await _dockerClient.Containers.InspectContainerAsync(mysqlContainer.ID);
                var mysqlHostname = mysqlContainerInfo.Config.Hostname;

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

                // Récupérez le "hostname" du conteneur WordPress après démarrage
                var wordpressContainerInfo = await _dockerClient.Containers.InspectContainerAsync(wordpressContainer.ID);
                var wordpressHostname = wordpressContainerInfo.Config.Hostname;

                // Enregistrez une entrée dans la base de données
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

                // Récupérer l'utilisateur à partir de la base de données
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Assurez-vous que l'ID utilisateur est défini dans le modèle
                model.UserId = userId.ToString();

                string netname = "network-" + model.UserId;
                string baseContnamePostgres = "postgres-container-" + model.UserId;
                string baseContnameOdoo = "odoo-container-" + model.UserId;
                string contnamePostgres = baseContnamePostgres;
                string contnameOdoo = baseContnameOdoo;

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
                await PullImageIfNotExists("postgres:latest");
                await PullImageIfNotExists("odoo:latest");

                // Trouvez des ports disponibles
                int availablePostgresPort = GetAvailablePort();
                int availableOdooPort = GetAvailablePort();

                // Vérifiez si le nom du conteneur PostgreSQL existe déjà et ajoutez un numéro si nécessaire
                int counter = 1;
                var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                while (containers.Any(c => c.Names.Contains("/" + contnamePostgres)))
                {
                    contnamePostgres = $"{baseContnamePostgres}-{counter}";
                    counter++;
                }

                // Créez et démarrez le conteneur PostgreSQL
                var postgresContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "postgres:latest",
                    Name = contnamePostgres,
                    Env = new List<string>
                    {
                        $"POSTGRES_PASSWORD={model.PostgresPassword}",
                        $"POSTGRES_DB=postgres",//{model.PostgresDatabase}",
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

                // Réinitialisez la liste des conteneurs pour le conteneur Odoo
                containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

                // Réinitialisez le compteur pour le conteneur Odoo
                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameOdoo)))
                {
                    contnameOdoo = $"{baseContnameOdoo}-{counter}";
                    counter++;
                }

                // Créez et démarrez le conteneur Odoo
                var odooContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "odoo:latest",
                    Name = contnameOdoo,
                    Env = new List<string>
                    {
                        $"HOST={contnamePostgres}",
                        $"PORT=5432",
                        $"POSTGRES_DB=postgres",//{model.PostgresDatabase}",
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

                // Récupérez le "hostname" du conteneur WordPress après démarrage
                var odooContainerInfo = await _dockerClient.Containers.InspectContainerAsync(odooContainer.ID);
                var odooHostname = odooContainerInfo.Config.Hostname;

                // Récupérez le "hostname" du conteneur WordPress après démarrage
                var postgreContainerInfo = await _dockerClient.Containers.InspectContainerAsync(postgresContainer.ID);
                var postgreHostname = postgreContainerInfo.Config.Hostname;


                // Enregistrez une entrée dans la base de données
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

                containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });

                counter = 1;
                while (containers.Any(c => c.Names.Contains("/" + contnameRedmine)))
                {
                    contnameRedmine = $"{baseContnameRedmine}-{counter}";
                    counter++;
                }

                var redmineContainer = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "redmine:latest",
                    Name = contnameRedmine,
                    Env = new List<string>
                    {
                        $"REDMINE_DB_MYSQL={contnameMySQL}",
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

                // Récupérez le "hostname" du conteneur WordPress après démarrage
                var redmineContainerInfo = await _dockerClient.Containers.InspectContainerAsync(redmineContainer.ID);
                var redmineHostname = redmineContainerInfo.Config.Hostname;

                // Récupérez le "hostname" du conteneur WordPress après démarrage
                var mysqlContainerInfo = await _dockerClient.Containers.InspectContainerAsync(mysqlContainer.ID);
                var mysqlHostname = mysqlContainerInfo.Config.Hostname;

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

                //var jwtHandler = new JwtSecurityTokenHandler();
                //var token = jwtHandler.ReadToken(Request.Headers["Authorization"].ToString().Replace("Bearer ", "")) as JwtSecurityToken;
                //var userId = Guid.Parse(token.Claims.First(claim => claim.Type == "nameid").Value);

                //// Récupérer l'utilisateur à partir de la base de données
                //var user = await _context.Users.FindAsync(userId);
                //if (user == null)
                //{
                //    return Unauthorized();
                //}

                //// Récupérer tous les conteneurs appartenant à cet utilisateur
                //var userContainers = await _context.ServerOrders
                //    .Where(so => so.UserId == userId)
                //    .ToListAsync();

                //foreach (var container in userContainers)
                //{
                //    // Arrêter et supprimer le conteneur WordPress
                //    await _dockerClient.Containers.StopContainerAsync(container.WordPressContainerId, new ContainerStopParameters());
                //    await _dockerClient.Containers.RemoveContainerAsync(container.WordPressContainerId, new ContainerRemoveParameters { Force = true });

                //    // Arrêter et supprimer le conteneur MySQL
                //    await _dockerClient.Containers.StopContainerAsync(container.MySQLContainerId, new ContainerStopParameters());
                //    await _dockerClient.Containers.RemoveContainerAsync(container.MySQLContainerId, new ContainerRemoveParameters { Force = true });

                //    // Supprimer les volumes associés aux conteneurs
                //    var containerDetails = await _dockerClient.Containers.InspectContainerAsync(container.WordPressContainerId);
                //    foreach (var mount in containerDetails.Mounts)
                //    {
                //        if (mount.Type == "volume")
                //        {
                //            await _dockerClient.Volumes.RemoveAsync(mount.Name, force: true);
                //        }
                //    }

                //    containerDetails = await _dockerClient.Containers.InspectContainerAsync(container.MySQLContainerId);
                //    foreach (var mount in containerDetails.Mounts)
                //    {
                //        if (mount.Type == "volume")
                //        {
                //            await _dockerClient.Volumes.RemoveAsync(mount.Name, force: true);
                //        }
                //    }

                //    // Supprimer le réseau associé
                //    string networkName = "network-" + container.UserId;
                //    var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters { Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", new Dictionary<string, bool> { { networkName, true } } } } });
                //    if (networks.Count > 0)
                //    {
                //        await _dockerClient.Networks.DeleteNetworkAsync(networks[0].ID);
                //    }

                //    // Supprimer l'entrée de la base de données
                //    _context.ServerOrders.Remove(container);
                //}

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

                var serverOrder = await _context.ServerOrders
                    .FirstOrDefaultAsync(so => so.Id == serverOrderId && so.UserId == userId);

                if (serverOrder == null)
                {
                    return NotFound();
                }

                string netname = "network-" + userId.ToString();

                // Supprimer l'entrée de la base de données
                _context.ServerOrders.Remove(serverOrder);
                await _context.SaveChangesAsync();

                // Suppression des conteneurs en fonction du type de service
                if (serverOrder.ServerType == "WordPress")
                {
                    await DeleteContainersAndResources(serverOrder.WordPressContainerId, serverOrder.MySQLContainerId, netname);
                }
                else if (serverOrder.ServerType == "Odoo")
                {
                    await DeleteContainersAndResources(serverOrder.OdooContainerId, serverOrder.OdooPostgreSQLContainerId, netname);
                }
                else if (serverOrder.ServerType == "Redmine")
                {
                    await DeleteContainersAndResources(serverOrder.RedmineContainerId, serverOrder.MySQLContainerId, netname);
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

            // Supprimer les volumes associés aux conteneurs
            //var volumes = await _dockerClient.Volumes.ListAsync();
            //foreach (var volume in volumes.Volumes)
            //{
            //    if (volume.Mountpoint.Contains(mainContainerId) || volume.Mountpoint.Contains(dbContainerId))
            //    {
            //        await _dockerClient.Volumes.RemoveAsync(volume.Name, force: true);
            //    }
            //}

            //// Supprimer le réseau associé
            //var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters());
            //var network = networks.FirstOrDefault(n => n.Name == networkName);
            //if (network != null)
            //{
            //    await _dockerClient.Networks.DeleteNetworkAsync(network.ID);
            //}
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
