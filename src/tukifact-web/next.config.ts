import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/v1/:path*",
        destination: "http://localhost:5186/v1/:path*",
      },
    ];
  },
};

export default nextConfig;
