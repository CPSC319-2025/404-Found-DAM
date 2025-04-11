## Frontend

### Requirements
- React (v18.2)
- Next.js (v14)
- Tailwind CSS (v4.0)

### Required packages
- See package.json file

### Dependencies
These dependencies are required for the application to run and can be found in the package.json file along with their respective versions:
- @emotion/react
- @emotion/styled
- @heroicons/react
- @mui/material
- @types/jszip
- multiselect-react-dropdown
- next
- pintura
- react
- react-dom
- react-dropzone
- react-easy-crop
- react-toastify

### Dev Dependencies
These dependencies are used during development and can also be found in the package.json file along with their respective versions. 
- @types/node
- @types/react
- @types/react-dom
- eslint
- eslint-config-next
- eslint-config-prettier
- eslint-plugin-prettier
- form-data
- gh-pages
- jszip
- lint-staged
- postcss
- prettier
- tailwindcss
- typescript
- zstd-codec
- zstd.ts

## Frontend Installation Documentation (Dev Environment)

First you must create a .env file in the current directory:
- for MAC
  ```bash
  touch .env
  ```
- for Windows
  ```
  New-Item -Path appsettings.json -ItemType File
  ```

Then copy the contents from the file provided in the final submission folder: env.dev

To install and run the Next.js application for local development run:

- for MAC
  ```bash
  ./run-frontend.sh
  ```

- for Windows
  ```bash
  .\run-frontend.ps1
  ```

The Next.js application should be running and reachable by navigating in the browser to http://localhost:3000.
