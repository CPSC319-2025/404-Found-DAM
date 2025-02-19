"use client";

import { useState } from "react";
import GenericForm from "@/app/components/GenericForm";

interface User {
  id: string;
  name: string;
  email: string;
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([
    { id: "1", name: "John Doe", email: "john@example.com" },
    { id: "2", name: "Jane Smith", email: "jane@example.com" },
  ]);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleAddUser = (newUser: { name: string; email: string }) => {
    setUsers([...users, { ...newUser, id: users.length + 1 }]);
    setIsModalOpen(false);
  };

  const fields: Field[] = [
    { name: "name", label: "Name", type: "text", placeholder: "Enter name", required: true },
    { name: "email", label: "Email", type: "email", placeholder: "Enter email", required: true },
  ];

  return (
    <div className="p-4">
      <h1 className="text-2xl font-bold mb-4">Users Page</h1>

      <button
        onClick={() => setIsModalOpen(true)}
        className="bg-blue-500 text-white p-2 rounded mb-4"
      >
        Add User
      </button>

      <div className="space-y-2">
        {users.map((user) => (
          <div key={user.id} className="flex justify-between items-center bg-gray-100 p-2 rounded">
            <span>{user.name} - {user.email}</span>
          </div>
        ))}
      </div>

      {isModalOpen && (
        <GenericForm
          title="Add New User"
          fields={fields}
          onSubmit={handleAddUser}
          onCancel={() => setIsModalOpen(false)}
          submitButtonText="Add User"
        />
      )}
    </div>
  );
}
