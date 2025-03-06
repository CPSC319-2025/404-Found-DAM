// src/app/layout.tsx
"use client";

import localFont from "next/font/local";
import { useEffect, useState } from "react";
import "./globals.css";
import Navbar from "@/app/components/Navbar";
import Login from "@/app/components/Login";
import { FileProvider } from "@/app/context/FileContext";
import { UserProvider } from "@/app/context/UserContext";
import { getUserFromToken, User } from "@/app/utils/api/auth";

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [loading, setLoading] = useState(true);
  const [user, setUser] = useState<any>(null);

  useEffect(() => {
    const fetchUserData = async () => {
      const user = await getUserFromToken();
      setUser(user);
      setLoading(false);
    };

    fetchUserData();

    return () => {
      setLoading(true);
    };
  }, []);

  const handleLogout = () => {
    setUser(null);
  };

  if (loading) {
    return (
      <html lang="en">
        <body
          className={`${geistSans.variable} ${geistMono.variable} antialiased`}
        >
          <div className="flex min-h-screen items-center justify-center">
            <p>Loading...</p>
          </div>
        </body>
      </html>
    );
  }

  if (!user) {
    return (
      <html lang="en">
        <body
          className={`${geistSans.variable} ${geistMono.variable} antialiased`}
        >
          <div className="flex min-h-screen items-center justify-center">
            <Login setUser={setUser} />
          </div>
        </body>
      </html>
    );
  }

  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <UserProvider value={{ user, setUser }}>
          <FileProvider>
            <div className="flex min-h-screen flex-col sm:flex-row">
              <Navbar />
              <main className="p-4 flex-1 mt-20 sm:mt-0 md:sm-64">
                {children}
              </main>
            </div>
          </FileProvider>
        </UserProvider>
      </body>
    </html>
  );
}
