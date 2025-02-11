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
