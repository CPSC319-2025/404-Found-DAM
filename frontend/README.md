### Requirements:
- npm

## Getting Started
```bash
npm install

npm run dev
```

### Cheatsheet:
- each folder in app/ specifies a path in the url (ex. 'localhost:3000/projects')
- a [slug] folder specifies the page for a dynamic route (ex. 'localhost:3000/projects/2')
- reusable components go in the @/components folder
- if you have a component specific to a certain page/path then put it in that folder (ex. ProjectCard.tsx goes in @/projects/components and not @/components/)
- global variables go in globals.css as well as tailwind.config.ts
- for icons: https://heroicons.com/



### For Hoi:

- Core -> entities -> DataModel.cs 
- To interact with the database, write a file for the 
  object in Infrastrcutrue/Dataaccess

  To interact with the database, write a file for the object in Infrastructure/DataAccess (eg. ActivityLogRepository)

APIs (to receive input from and provide resposnse to frontend) go into the APIs/Controllers folder: ActivityLogController

Logic and any functionality that does not need to access database goes into Service (Core/Services)

FLOW:

    Frontend calls an endpoint (in ActivityLogController).
    Controller calls Service.
    Service calls DataAccess/Repository (if necessary).
    DataAccess/Repository returns to Service
    Service returns to Controller
    Controller returns to frontend.

﻿