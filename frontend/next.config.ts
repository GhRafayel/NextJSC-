import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    output: "standalone",
    reactCompiler: true,
    allowedDevOrigins: ['192.168.0.201', 'localhost'],
    experimental: {
      turbopackFileSystemCacheForDev: true,
    },
    images: {
      dangerouslyAllowSVG: true,
      contentDispositionType: 'attachment',
      contentSecurityPolicy: "default-src 'self'; script-src 'none'; sandbox;",
      remotePatterns: [
        {
          protocol: 'http',
          hostname: 'localhost',
          port: '4000',
          pathname: '/uploads/**',
        },
      ],
    },
};

export default nextConfig;
