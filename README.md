# Borderland Dreams platform

Create **Clouds** that users can create their **Dreams** in.  
Each dream has a **Main Dreamer** and can also have multiple **Co-dreamers**.  
Dreamers can add **Budget Items** to their dreams.
Only a main dreamer can delete the dream.

A **Funding Round** is a period where users (that has an active BL membership) gets **Funds** in their **Wallets**, to give to dreams. 

## Getting The Project Started

### Prerequisites

- [.NET 8.0](https://dotnet.microsoft.com/download)

### Build and Run

1. Restore packages and build
    ```sh
    dotnet restore
    dotnet build
    ````

2. Create/update database
    ```sh
    dotnet ef database update
    ````

3. Run the app
    ````
    dotnet run
    ````

### General info
* SQLite database
* Using [Entity Framework (EF) Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli) for database stuff
* An admin user is created in development mode, see [DataSeeder.cs](Data/DataSeeder.cs)
