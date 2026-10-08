# TUI

Een .NET 10 Blazor-app met een JSON-API, EF Core en MariaDB. De API ondersteunt testrecords, accountregistratie/inloggen, rolgebaseerd gebruikersbeheer en handmatige factuurregistratie.

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop met Docker Compose (voor de lokale database en integratietest)
- EF CLI voor nieuwe migraties: `dotnet tool install --global dotnet-ef --version 10.0.12`

## Lokaal starten met Docker

De Compose-configuratie bouwt de app uit deze checkout en start een alleen intern bereikbare MariaDB-container. De testdatabase gebruikt bewust de ingecheckte testgegevens; gebruik dit bestand niet voor productie.

```bash
docker compose up --build -d
docker compose ps
```

Open daarna [http://localhost:4675](http://localhost:4675). De eerste geregistreerde gebruiker ontvangt de rol `Admin`; volgende registraties krijgen de rol `User`. Een admin kan op **Gebruikers** rollen wijzigen.

Stop de lokale omgeving met:

```bash
docker compose down
```

## Zonder Docker starten

Stel de databaseverbinding buiten de repository in met user secrets of omgevingsvariabelen. Een volledige connection string heeft voorrang op de losse `Db`-waarden.

```bash
dotnet user-secrets set "ConnectionStrings:MariaDb" "Server=localhost;Port=3306;Database=tui;User=tui;Password=vervang-mij"
dotnet watch
```

Hetzelfde kan via `ConnectionStrings__MariaDb` of via `Db__host`, `Db__port`, `Db__name`, `Db__user` en `Db__password`.

## API

Alle API-fouten zijn `application/problem+json`. Validatiefouten geven `400 Bad Request`; ongeldige inloggegevens geven `401 Unauthorized`; dubbele accounts geven `409 Conflict`.

| Methode | Route | Toegang | Doel |
| --- | --- | --- | --- |
| `POST` | `/api/test-records` | publiek | Testrecord opslaan |
| `GET` | `/api/test-records` | publiek | Testrecords ophalen |
| `GET` | `/api/recipients` | ingelogd | Bestaande ontvangers ophalen |
| `POST` | `/api/invoices` | ingelogd | Factuur handmatig opslaan |
| `GET` | `/api/invoices` | ingelogd | Factuuroverzicht ophalen |
| `GET` | `/api/invoices/{internalReference}` | ingelogd | Factuur opnieuw openen |
| `POST` | `/api/auth/register` | publiek | Account registreren |
| `POST` | `/api/auth/login` | publiek | Inloggen en sessiecookie ontvangen |
| `POST` | `/api/auth/logout` | ingelogd | Uitloggen |
| `GET` | `/api/auth/me` | ingelogd | Huidige gebruiker ophalen |
| `GET` | `/api/users` | Admin | Gebruikersoverzicht ophalen |
| `PUT` | `/api/users/{id}/role` | Admin | Rol wijzigen naar `User` of `Admin` |

Voorbeeld van een testrecord:

```bash
curl -X POST http://localhost:4675/api/test-records \
  -H "Content-Type: application/json" \
  -d '{"name":"Eerste testrecord"}'
```

Een ingelogde medewerker kan in de interface via **Facturen** een factuur vastleggen. Voor lokaal gebruik zijn twee voorbeeldontvangers beschikbaar. Ontvangers aanmaken valt buiten deze applicatiestroom; de factuur kiest altijd een bestaand record.

## Migraties

Migraties worden bij het starten automatisch toegepast. Maak een nieuwe migratie met:

```bash
dotnet ef migrations add <naam>
```

## Integratietest

De integratietesten starten een tijdelijke MariaDB-container. Zij controleren zowel de testrecord- als de factuurstroom: opslaan via `POST`, ophalen via `GET`, interne referentie en de status `Openstaand`. Docker Desktop moet actief zijn.

```bash
dotnet test tui.IntegrationTests/tui.IntegrationTests.csproj
```
