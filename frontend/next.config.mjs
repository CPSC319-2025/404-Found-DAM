/** @type {import('next').NextConfig} */
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,
  },
  webpack: (config) => {
    // Stub out `fs` so that packages referencing Node's `fs` won't break
    config.resolve.fallback = {
      ...config.resolve.fallback,
      fs: false,
    };
    return config;
  },
};

// Uncomment if you need these export/basePath settings
// nextConfig.output = 'export';
// nextConfig.basePath = '/404-Found-DAM';
// nextConfig.assetPrefix = '/404-Found-DAM';

export default nextConfig;
