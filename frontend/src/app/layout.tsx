"use client";

import localFont from "next/font/local";
import { useEffect, useState, useRef } from "react";
import "./globals.css";
import Navbar from "@/app/components/Navbar";
import Login from "@/app/components/Login";
import { FileProvider } from "@/app/context/FileContext";
import { UserProvider } from "@/app/context/UserContext";
import { getUserFromToken, User } from "@/app/utils/api/auth";
import React from "react";
import { ToastContainer } from "react-toastify";
import { useUser } from "@/app/context/UserContext";
import { logout } from "@/app/utils/api/auth";
import { CogIcon } from "@heroicons/react/24/solid";

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
  const [user, setUser] = useState<User | null>(null);
  const [isDropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const handleLogout = () => {
    logout();
    setUser(null);
    setDropdownOpen(false); // Close the dropdown after logout
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setDropdownOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

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
            <ToastContainer autoClose={5000} position="top-center" />
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
            <div className="flex min-h-screen flex-col sm:flex-row relative">
              <Navbar />
              <div className="fixed bottom-4 right-4 block z-50" ref={dropdownRef}>
                <div className="relative">
                  <button
                    ref={buttonRef}
                    onClick={() => setDropdownOpen(!isDropdownOpen)}
                    className="bg-gray-300 p-2 rounded-full hover:bg-gray-300"
                  >
                    <CogIcon className="w-4 h-4 sm:w-6 sm:h-6 text-gray-700" />
                  </button>

                  {isDropdownOpen && (
                    <div className="absolute right-0 bottom-full mb-2 bg-white border rounded shadow-md w-32">
                      <button
                        onClick={handleLogout}
                        className="w-full text-left p-2 text-red-600 hover:bg-gray-100"
                      >
                        Logout
                      </button>
                    </div>
                  )}
                </div>
              </div>
              <main className="p-4 flex-1 mt-16 sm:mt-0 md:sm-64">
                {children}
              </main>
            </div>
            <ToastContainer autoClose={5000} position="top-center" />
          </FileProvider>
        </UserProvider>
      </body>
    </html>
  );
}
