import Link from "next/link";
import { ReactNode, useState } from "react";
import { ArrowUpTrayIcon } from "@heroicons/react/24/solid";
import {
  RectangleGroupIcon,
  ChartBarIcon,
  Bars3Icon,
  XMarkIcon,
} from "@heroicons/react/24/outline";

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
  // ...(isSuperAdmin
  //   ? [
  //       {
  //         path: "users",
  //         title: "Users",
  //         mobileTitle: "Users",
  //         icon: <UsersIcon className="w-8 h-8 sm:w-8 sm:h-8" />,
  //       },
  //     ]
  //   : []),
];
export default function Navbar() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <>
      {/* Mobile */}
      <nav className="sm:hidden bg-navbar p-2 fixed top-0 left-0 right-0 z-50 flex justify-between items-center">
        <button
          onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          className="p-2"
          aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
        >
          {mobileMenuOpen ? (
            <XMarkIcon className="w-8 h-8" />
          ) : (
            <Bars3Icon className="w-8 h-8" />
          )}
        </button>
        <Link href="https://www.ae.ca/">
          <img
            src="/images/ae_logo_blue.svg"
            alt="Associated Engineering"
            className="w-16 h-16"
          />
        </Link>
      </nav>

      {mobileMenuOpen && (
        <div
          className={`sm:hidden fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity ease-in-out duration-500 ${
            mobileMenuOpen
              ? "opacity-100 pointer-events-auto"
              : "opacity-0 pointer-events-none"
          }`}
          onClick={() => setMobileMenuOpen(false)}
        >
          <div
            className={`absolute left-0 top-0 bg-navbar w-64 h-full p-4 transition-transform ease-in-out duration-500 ${
              mobileMenuOpen ? "translate-x-0" : "-translate-x-full"
            }`}
            onClick={(e) => e.stopPropagation()}
          >
            <ul className="space-y-4 mt-20">
              {pages.map((page) => (
                <li key={page.path}>
                  <Link
                    href={`/${page.path}`}
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center p-2 rounded hover:bg-gray-200"
                  >
                    <span className="mr-2">{page.icon}</span>
                    <span>{page.title}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}

      {/* Desktop */}
      <nav className="hidden sm:block group transition-all duration-300 w-24 hover:w-64 bg-navbar p-2 fixed sm:relative z-50">
        <div className="pl-3">
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
    </>
  );
}
