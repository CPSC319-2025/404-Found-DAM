"use client";

import { useState } from "react";
import { login } from "@/app/utils/api/auth";
import { getUserFromToken } from "@/app/utils/api/auth";

interface LoginProps {
  setUser: (user: any) => void;
}

export default function Login({ setUser }: LoginProps) {
  const [email, setEmail] = useState("");

  const handleLogin = async () => {
    const token = await login(email);
    if (token) {
      const user = await getUserFromToken();
      setUser(user);
    } else {
      alert("No user with given email");
    }
  };

  return (
    <div className="login-container">
      <input
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="Enter your email"
        className="input-email"
      />
      <button onClick={handleLogin} className="btn-login">
        Log In
      </button>
    </div>
  );
}
