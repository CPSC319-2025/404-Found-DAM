import React, { createContext, useContext, ReactNode } from "react";
import { User } from "@/app/utils/api/auth";

interface UserContextType {
  user: User | null;
  setUser: (user: User | null) => void;
}

const UserContext = createContext<UserContextType | undefined>(undefined);

interface UserProviderProps {
  value: UserContextType;
  children: ReactNode;
}

export const UserProvider = ({ value, children }: UserProviderProps) => {
  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
};

export const useUser = () => {
  const context = useContext(UserContext);
  if (!context) {
    throw new Error("useUser must be used within a UserProvider");
  }
  return context;
};
