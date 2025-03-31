import Link from "next/link";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { Menu, MenuItem, IconButton } from "@mui/material";
import { EllipsisVerticalIcon } from "@heroicons/react/16/solid";
import { useUser } from "@/app/context/UserContext";
import { User } from "@/app/types";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { convertUtcToLocal } from "@/app/utils/api/getLocalTime";

interface ProjectCardProps {
  id: string;
  name: string;
  creationTime: string;
  assetCount: number;
  admins: User[];
  userNames: string[];
}

export default function ProjectCard({
  id,
  name,
  creationTime,
  assetCount,
  admins,
  userNames,
}: ProjectCardProps) {
  const { user } = useUser();
  const router = useRouter();
  const formattedCreationTime = convertUtcToLocal(creationTime);

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);

  const [hasAdminAccess, setHasAdminAccess] = useState<boolean>(false);

  const handleMenuOpen = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = (event?: any) => {
    event?.stopPropagation();
    setAnchorEl(null);
  };

  const handleCardClick = () => {
    router.push(`/projects/${id}`);
  };

  const handleEdit = (event: any) => {
    event.stopPropagation();
    router.push(`/projects/edit/${id}`);
    handleMenuClose();
  }

  const handleArchive = (event: any) => {
    event.stopPropagation();
    handleMenuClose();
  }

  const handleExport = async (event: any) => {
    event.stopPropagation();
    try {
      const response = await fetchWithAuth(`projects/${id}/export`, {
        method: "POST",
        headers: {
          "Accept": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to export project: ${response.statusText}`);
      }

      const blob = await response.blob();

      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = `${name.replace(/\s+/g, '_')}.xlsx`;
      link.click();
    } catch (error) {
      toast.error((error as Error).message);
    }
    handleMenuClose();
  }

  useEffect(() => {
    if (user?.superadmin) {
      setHasAdminAccess(true);
    } else {
      if (admins?.find(admin => admin.userID === user!.userID)) {
        setHasAdminAccess(true);
      }
    }
  }, [admins])

  return (
    <div
      className="border p-4 rounded-lg transition-shadow duration-300 bg-white shadow-sm hover:cursor-pointer"
      onClick={handleCardClick}
    >
      <div className="flex flex-col gap-4 p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                fill="currentColor"
                className="size-6"
              >
                <path d="M19.5 21a3 3 0 0 0 3-3v-4.5a3 3 0 0 0-3-3h-15a3 3 0 0 0-3 3V18a3 3 0 0 0 3 3h15ZM1.5 10.146V6a3 3 0 0 1 3-3h5.379a2.25 2.25 0 0 1 1.59.659l2.122 2.121c.14.141.331.22.53.22H19.5a3 3 0 0 1 3 3v1.146A4.483 4.483 0 0 0 19.5 9h-15a4.483 4.483 0 0 0-3 1.146Z" />
              </svg>
            </div>
            <div>
              <p className="text-l font-semibold">
                <Link
                  href={`/projects/${id}`}
                  passHref
                  className="text-blue-500"
                >
                  {name}
                </Link>
              </p>
              <p className="text-sm text-gray-500">{formattedCreationTime}</p>
            </div>
          </div>
          {hasAdminAccess && (
            <IconButton onClick={handleMenuOpen} className="flex items-center justify-center w-8 h-8 hover:bg-gray-100 rounded-full cursor-pointer">
              <EllipsisVerticalIcon />
            </IconButton>
          )}

          <Menu anchorEl={anchorEl} open={open} onClose={handleMenuClose}>
            <MenuItem onClick={handleEdit}>
              Edit
            </MenuItem>
            <MenuItem
              onClick={handleArchive}
            >
              Archive
            </MenuItem>
            <MenuItem
              onClick={handleExport}
            >
              Export
            </MenuItem>
          </Menu>
        </div>
        <div>
          <div className="flex justify-between items-center">
            <p className="text-sm text-gray-500">Shared Users</p>
            <p className="text-sm text-gray-500">Inside Files</p>
          </div>
          <div className="flex items-center justify-between mt-2">
            <div className="relative flex -space-x-2">
              {userNames.slice(0, 4).map((username, index) => (
                <div
                  key={index}
                  className="w-8 h-8 rounded-full bg-gray-200 border-2 border-white flex items-center justify-center"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    className="w-5 h-5 text-gray-600"
                  >
                    <path
                      fillRule="evenodd"
                      d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z"
                      clipRule="evenodd"
                    />
                  </svg>
                </div>
              ))}
              {userNames.length > 4 && (
                <div className="w-8 h-8 rounded-full bg-blue-100 border-2 border-white flex items-center justify-center text-sm font-medium text-blue-600">
                  +{userNames.length - 4}
                </div>
              )}
            </div>
            <div className="w-16 h-8 rounded-md bg-blue-100 border-2 border-white flex items-center justify-center text-sm font-medium text-blue-600">
              {assetCount}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
