# Procédure d'Installation sur Ubuntu

Cette procédure sert de processus de redéploiement en cas de problème sur un serveur Ubuntu. Elle couvre l'installation de l'API ASP.NET Core, du front-end Angular, de PostgreSQL, ainsi que la configuration réseau avec un reverse proxy pour supporter les infrastructures IPv4 et IPv6. Toutes les commandes nécessaires sont listées, y compris le script `webmason.service` pour assurer le redémarrage des services en cas de redémarrage du serveur.

## Prérequis

- Un serveur Ubuntu 20.04 LTS ou supérieur.
- Accès root ou sudo au serveur.
- Nom de domaine configuré pointant vers votre serveur (optionnel mais recommandé).

## Mise à Jour du Système

```bash
sudo apt update
sudo apt upgrade -y
```
## Installation de PostgreSQL
### Installer PostgreSQL :

```bash
sudo apt install postgresql postgresql-contrib -y
```
Configurer l'utilisateur et la base de données :

```bash
Copier le code
sudo -u postgres psql
Dans le shell PostgreSQL :
```
```sql
CREATE DATABASE webmason;
CREATE USER webmason WITH ENCRYPTED PASSWORD 'mot_de_passe_choisi';
GRANT ALL PRIVILEGES ON DATABASE webmason TO webmason;
```
## Installation du Runtime .NET Core
### Ajouter le dépôt Microsoft :

```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```
### Installer le SDK .NET Core :

```bash
sudo apt update
sudo apt install apt-transport-https -y
sudo apt install dotnet-sdk-6.0 -y
```
## Déploiement de l'API ASP.NET Core
### Cloner votre projet API :

```bash
git clone https://github.com/WebMason-CPLR/WebMason-final/tree/667616f81b5c3ee0134fef65ac3ee848c5367a45/WebMason-final.Server /var/www/api
```
### Publier l'application :

```bash
cd /var/www/api
dotnet publish -c Release -o out
```

### Application des Migrations Entity Framework
## Installer les outils Entity Framework Core :

```bash
dotnet tool install --global dotnet-ef
#Note : Si vous avez déjà installé dotnet-ef, vous pouvez passer cette étape.
```

## Appliquer les migrations à la base de données :

```bash
cd /var/www/api
dotnet ef database update --project ./webmason_final.csproj --startup-project ./webmason_final.csproj
```

## Installation de Node.js et Angular CLI
### Installer Node.js :

```bash
curl -fsSL https://deb.nodesource.com/setup_16.x | sudo -E bash -
sudo apt install -y nodejs
```
### Installer Angular CLI :

```bash
sudo npm install -g @angular/cli
```
## Déploiement du Front-End Angular
### Cloner le projet Angular :

```bash
git clone https://github.com/WebMason-CPLR/WebMason-final/tree/667616f81b5c3ee0134fef65ac3ee848c5367a45/webmason-final.client /var/www/front
```
### Installer les dépendances et construire l'application :

```bash
cd /var/www/front
npm install
ng build --prod
```
### Déplacer les fichiers construits vers le dossier public :

```bash
sudo mv webmason-final.client/src/* /var/www/html/
```

## Configuration de Nginx comme Reverse Proxy
### Installer Nginx :

```bash
sudo apt install nginx -y
```

### Configurer Nginx pour l'API et le Front-End :

```bash
sudo nano /etc/nginx/sites-available/default
```
configuration :

```nginx
server {
    listen 80 default_server;
    listen [::]:80 default_server;

    server_name webmason.fr;

    # Configuration pour le Front-End Angular
    root /var/www/html;
    index index.html index.htm;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Reverse Proxy pour l'API ASP.NET Core
    location /api/ {
        proxy_pass http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```
Tester et recharger Nginx :

```bash
sudo nginx -t
sudo systemctl restart nginx
```
### Configuration IPv4 et IPv6
### Nginx doit écouter sur les deux protocoles :

```nginx
Copier le code
listen 80 default_server;
listen [::]:80 default_server;
```
## Création du Service systemd webmason.service
### Créer le fichier de service :

```bash
sudo nano /etc/systemd/system/webmason.service
```
Ajouter le contenu suivant :

```ini
[Unit]
Description=WebMason Service
After=network.target

[Service]
Type=simple
ExecStart=/usr/bin/dotnet /var/www/api/out/webmason_final.dll
WorkingDirectory=/var/www/api/out
Restart=always
RestartSec=10
SyslogIdentifier=webmason-service
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```
Recharger systemd et activer le service :

```bash
sudo systemctl daemon-reload
sudo systemctl enable webmason.service
sudo systemctl start webmason.service
```
Ce service démarre l'API ASP.NET Core et assure son redémarrage automatique en cas de panne ou de redémarrage du serveur.
