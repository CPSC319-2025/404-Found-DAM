import Link from "next/link";
import { ReactNode } from "react";
import {
  ViewfinderCircleIcon,
  ChartBarIcon,
  ArrowUpTrayIcon,
  UsersIcon,
  UserIcon,
} from "@heroicons/react/24/solid";

interface Page {
  path: string;
  title: string;
  mobileTitle: string;
  icon: ReactNode;
}

const isSuperAdmin = true;

const pages: Page[] = [
  {
    path: "projects",
    title: "Projects",
    mobileTitle: "Projects",
    icon: <ViewfinderCircleIcon className="w-8 h-8 sm:w-6 sm:h-6" />,
  },
  {
    path: "activity",
    title: "Activity Monitor",
    mobileTitle: "Activity",
    icon: <ChartBarIcon className="w-8 h-8 sm:w-6 sm:h-6" />,
  },
  {
    path: "upload",
    title: "Upload Palette",
    mobileTitle: "Upload",
    icon: <ArrowUpTrayIcon className="w-8 h-8 sm:w-6 sm:h-6" />,
  },
  ...(isSuperAdmin
    ? [
      {
        path: "users",
        title: "Users",
        mobileTitle: "Users",
        icon: <UsersIcon className="w-8 h-8 sm:w-6 sm:h-6" />,
        },
    ]
    : []),
];

export default function Navbar() {
  return (
    <nav className="w-full sm:w-64 bg-navbar p-2 fixed sm:relative z-50">
      <div className="block sm:hidden">
        <ul className="flex justify-around space-x-4">
          {pages.map((page) => (
            <li key={page.path} className="text-center">
              <Link
                href={`/${page.path}`}
                className="flex flex-col items-center hover:bg-gray-200 p-2 rounded"
              >
                <span className="mr-2">{page.icon}</span>
                <span className="text-xs">{page.mobileTitle}</span>
              </Link>
            </li>
          ))}
        </ul>
      </div>

      <div className="hidden sm:block">
        <h2 className="text-xl font-bold mb-4">AE</h2>
        <ul className="space-y-2">
          {pages.map((page) => (
            <li key={page.path}>
              <Link
                href={`/${page.path}`}
                className="flex items-center hover:bg-gray-200 p-2 rounded"
              >
                <span className="mr-2">{page.icon}</span>
                {page.title}
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </nav>
  );
}
