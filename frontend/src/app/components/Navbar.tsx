import Link from "next/link";
import { ReactNode } from "react";
import {
  ViewfinderCircleIcon,
  ArrowUpTrayIcon,
  UsersIcon,
  UserIcon,
} from "@heroicons/react/24/solid";
import { RectangleGroupIcon, ChartBarIcon } from "@heroicons/react/24/outline";

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
    icon: <RectangleGroupIcon className="w-8 h-8 sm:w-8 sm:h-8" />,
  },
  {
    path: "activity",
    title: "Activity Monitor",
    mobileTitle: "Activity",
    icon: <ChartBarIcon className="w-8 h-8 sm:w-8 sm:h-8" />,
  },
  {
    path: "palette",
    title: "Upload Palette",
    mobileTitle: "Upload",
    icon: <ArrowUpTrayIcon className="w-8 h-8 sm:w-8 sm:h-8" />,
  },
  ...(isSuperAdmin
    ? [
        {
          path: "users",
          title: "Users",
          mobileTitle: "Users",
          icon: <UsersIcon className="w-8 h-8 sm:w-8 sm:h-8" />,
        },
      ]
    : []),
];
export default function Navbar() {
  return (
    <nav className="group transition-all duration-300 w-24 hover:w-64 bg-navbar p-2 fixed sm:relative z-50">
      <div className="block sm:hidden">
        <ul className="flex justify-around space-x-4">
          {pages.map((page) => (
            <li key={page.path} className="text-center">
              <Link
                href={`/${page.path}`}
                className="flex flex-col items-center hover:bg-gray-200 p-2 rounded"
              >
                <span>{page.icon}</span>
                <span className="text-xs">{page.mobileTitle}</span>
              </Link>
            </li>
          ))}
        </ul>
      </div>

      <div className="hidden sm:block pl-3">
        <div className="flex justify-center mb-4 overflow-hidden">
          <Link href="https://www.ae.ca/">
            <img
              src="/images/ae_logo_blue.svg"
              alt="Associated Engineering"
              className="w-24 h-24 transition-all duration-300 transform scale-50 opacity-0 group-hover:scale-100 group-hover:opacity-100"
            />
          </Link>
        </div>
        <ul className="space-y-10">
          {pages.map((page) => (
            <li key={page.path}>
              <Link
                href={`/${page.path}`}
                className="flex items-center p-2 rounded text-gray-500 hover:text-blue-500 px-3"
              >
                <span className="mr-2">{page.icon}</span>
                <span className="text-lg whitespace-nowrap transition-all duration-300 opacity-0 group-hover:opacity-100 pl-2">
                  {page.title}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </nav>
  );
}
