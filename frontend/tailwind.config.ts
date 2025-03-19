import type { Config } from "tailwindcss";

const config: {
  plugins: any[];
  theme: { extend: { colors: { navbar: string; background: string; foreground: string } } };
  content: string[]
} = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        background: "var(--background)",
        foreground: "var(--foreground)",
        navbar: "var(--navbar-bg)",
      },
    },
  },
  plugins: [],
};
export default config;
