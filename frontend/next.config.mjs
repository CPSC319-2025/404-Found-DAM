// Uncomment these if you need a static export with a specific basePath/assetPrefix
// const nextConfig = {
//   output: 'export',
//   basePath: '/404-Found-DAM',
//   assetPrefix: '/404-Found-DAM',
// };

/** @type {import('next').NextConfig} */
const nextConfig = {
  // Example: ignore lint errors during build
  eslint: {
    ignoreDuringBuilds: true,
  },

  // Webpack configuration
  webpack: (config, { isServer }) => {
    // Only modify the client-side build
    if (!isServer) {
      // Provide a fallback for 'fs' so Next.js won’t fail on browser bundles
      config.resolve.fallback = {
        ...config.resolve.fallback,
        fs: false,
      };
    }
    return config;
  },

  images: {
    domains: ['assetsblobstandard.blob.core.windows.net'],
  },
};

export default nextConfig;