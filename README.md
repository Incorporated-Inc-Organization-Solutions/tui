## Start het project
```bash
dotnet watch
```

## Setup database verbinding
### Als je xampp gebruikt hoef je dit niet te doen.
```bash
dotnet user-secrets set "Db:password" ""
dotnet user-secrets set "Db:host" "localhost"
dotnet user-secrets set "Db:port" 3306
dotnet user-secrets set "Db:name" "tidawnloader"
dotnet user-secrets set "Db:user" "root"
```