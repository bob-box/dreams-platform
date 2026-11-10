
# Borderland Dreams platform

Changes to the main branch will be deployed to the test environment http://bl-dreams.runasp.net/

## What and how it works

***Clouds*** is a place where ***Dreams*** that belong together are happening.  

Each dream has a ***Main Dreamer***, but it can also have multiple ***Co-dreamers*** (only the main dreamer can delete their own dream).  
Dreamers can describe their dream and realize it by adding ***Budget Items***.

To create a dream a user must first ***Join The Cloud*** and create their dream during the ***Dreaming Period***, and they can edit until the ***Dream Edit Deadline***.

A user must also ***Join The Cloud***(and have an active BL membership status STILL A TODO) before they can participate in funding rounds.

A ***Funding Round*** is a period between two dates where users automatically gets ***Funds*** in their ***Wallets***, to give to dreams. A funding round sets all dreams that are in ***ReadyForFunding*** status to ***Active***.

When a funding round is over, all unspent funds get removed from the wallets and the cloud fund stats gets updated.

## Outbound email

Outbound email is sent through Resend. Set the API key as the `Resend__ApiKey` environment variable; do not put it in source control. Configure the verified sender address/name from **Admin Control Panel → Site-wide settings → Email**.

The send email page is available for admins within a cloud and is scoped to that cloud's dreams. It supports immediate or scheduled messages to all dreams, selected dreams, or dreams in a chosen status. At most one message per unique email address, they are of Resend email type "Transactional email" and are sent in batches. Personalized and/or event-driven communication, for example confirmation and password/account messages are also of Resend email type "Transactional email".


Dream creators receive an automatic welcome email after creating a dream. 
Each cloud can customize and enable or disable its dream welcome email in **Admin Control Panel → Edit Cloud**. Use `{DreamName}` in either field to insert the dream's name.


## Admins
Admin can
1. Create clouds
1. Create, edit, publish and hide any dream at any-ish time
1. Access the Admin Control Panel in a cloud to manage  
    1. Cloud settings
    1. Users and wallets (you can manually update wallets)
    1. Tags
    1. Funding rounds (will automatically start/end on set time)


## Getting The Project Started

### Prerequisites

- [.NET 10](https://dotnet.microsoft.com/download)

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
* Using [Entity Framework (EF) Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli) for database stuff
* An admin and normal user is created in development mode (also some test data), see [DataSeeder.cs](Data/DataSeeder.cs)
* More test data can be generated in the admin control panel menu
* Text editor for page info and dream description [Wysi](https://wysi.js.org) is a simple WYSIWYG text editor (What You See Is What You Get)