"use client";

import { useUser } from "@/app/context/UserContext";
import { logout } from "@/app/utils/api/auth";

export default function ProfilePage() {
  const { user, setUser } = useUser();

  const handleLogout = () => {
    logout();
    setUser(null);
<<<<<<< HEAD
  };
=======
  }
>>>>>>> main

  return (
    <div>
      <button
        onClick={handleLogout}
        className="bg-red-500 text-white p-2 rounded-md md:ml-4 sm:w-auto"
      >
        Logout
      </button>
    </div>
  );
}
