"use client";

import { useState } from "react";
import { login, getUserFromToken } from "@/app/utils/api/auth";
import { EyeIcon, EyeSlashIcon } from "@heroicons/react/24/outline";
import { toast } from "react-toastify";

interface LoginProps {
  setUser: (user: any) => void;
}

export default function Login({ setUser }: LoginProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault(); // Prevents page reload
    try {
      await login(email, password);
      const user = await getUserFromToken();
      setUser(user);
    } catch (error) {
      toast.error((error as Error).message, { toastId: "LoginError" });
    }
  };

  return (
    <div className="login-container">
      <form
        onSubmit={handleLogin}
        className="flex flex-col p-6 shadow-lg w-full max-w-sm bg-white rounded-2xl h-full space-y-2 justify-center items-center hover:scale-105 transition-transform duration-200"
      >
        <h2 className="text-2xl font-semibold text-center mb-4 text-gray-600 pb-5">
          Sign In
        </h2>
        <div className="flex flex-col space-y-5 m-4">
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Enter your email"
            className="border border-gray-600 rounded-md p-2 mx-3 focus:outline-none focus:ring-1 focus:ring-blue-400"
            required
          />
          <div className="relative w-full">
            <input
              type={showPassword ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              className="border border-gray-600 rounded-md p-2 mx-3 focus:outline-none focus:ring-1 focus:ring-blue-400"
              required
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute inset-y-0 right-4 text-gray-600"
            >
              {showPassword ? (
                <EyeSlashIcon className="size-5 inset-y-0 flex items hover:scale-105 transition-transform duration-200" />
              ) : (
                <EyeIcon className="size-5 inset-y-0 flex items hover:scale-105 transition-transform duration-200" />
              )}
            </button>
          </div>
        </div>
        <div className="w-full y-full flex justify-center items-center pt-6 pb-3">
          <button
            type="submit" // Ensures form submission
            className="bg-blue-400 rounded-md w-4/5 py-1.5 hover:scale-105 transition-transform duration-200 hover:bg-blue-500 text-gray-100"
          >
            LOGIN
          </button>
        </div>
      </form>
    </div>
  );
}
