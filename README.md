# Borderland Dreams platform

***Clouds*** is a place where ***Dreams*** that belong together are happening.  

Each dream has a ***Main Dreamer***, but it can also have multiple ***Co-dreamers*** (only the main dreamer can delete their own dream).  
Dreamers can describe their dream and realize it by adding ***Budget Items***.

To create a dream a user must first ***Join The Cloud*** and create their dream during the ***Dreaming Period***, and they can edit until the ***Dream Edit Deadline***.

A user must also ***Join The Cloud***(and have an active BL membership status STILL A TODO) before they can participate in funding rounds.

A ***Funding Round*** is a period between two dates where users automatically gets ***Funds*** in their ***Wallets***, to give to dreams.

When a funding round is over, all unspent funds get removed from the wallets and the cloud fund stats gets updated.


## Admins
Admin can
1. Create clouds
1. Edit any dream at any time
1. Access the Admin Control Panel in a cloud to manage  
    1. Cloud settings
    1. Users and wallets
    1. Tags
    1. Funding rounds


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
* An admin and normal user is created in development mode, see [DataSeeder.cs](Data/DataSeeder.cs)
* Test data is also seeded to the database in [DataSeeder.cs](Data/DataSeeder.cs)
