import Link from "next/link";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { Menu, MenuItem, IconButton } from "@mui/material";
import { EllipsisVerticalIcon, FolderIcon, ArchiveBoxArrowDownIcon, UserIcon } from "@heroicons/react/16/solid";
import { useUser } from "@/app/context/UserContext";
import { User } from "@/app/types";
import { fetchWithAuth } from "@/app/utils/api/api";
import { toast } from "react-toastify";
import { convertUtcToLocal } from "@/app/utils/api/getLocalTime";
import PopupModal from "@/app/components/ConfirmModal";

interface ProjectCardProps {
  id: string;
  name: string;
  archived: boolean;
  creationTime: string;
  assetCount: number;
  admins: User[];
  userNames: string[];
}

export default function ProjectCard({
  id,
  name,
  archived,
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

  const [confirmArchivePopup, setConfirmArchivePopup] = useState<boolean>(false);

  const [isArchived, setIsArchived] = useState(archived);

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

  const showConfirmArchive = (event: any) => {
    event.stopPropagation();
    handleMenuClose();
    setConfirmArchivePopup(true);
  }

  const closeConfirmArchivePopup = () => {
    setConfirmArchivePopup(false);
    handleMenuClose();
  }

  const onConfirmArchive = () => {
    setConfirmArchivePopup(false);
    handleArchive();
  }

  const handleArchive = async () => {
    try {
      const response = await fetchWithAuth("projects/archive", {
        method: "PATCH",
        body: JSON.stringify({ projectID: Number(id) })
      })

      if (response.status === 400) {
        throw new Error(`Project "${name}" already archived`);
      }
      
      if (!response.ok) {
        throw new Error(`Failed to archive project: ${response.statusText}`);
      }

      toast.success("Successfully archived project.")

      setIsArchived(true);
    } catch (error) {
      toast.error((error as Error).message);
    }

    handleMenuClose();
  }

  const handleExport = async (event: any) => {
    event.stopPropagation();
    toast.success("Exporting project...");
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
      className={`border p-4 rounded-lg transition-shadow duration-300 shadow-sm hover:cursor-pointer relative ${isArchived ? "bg-gray-100" : "bg-white"}`}
      onClick={handleCardClick}
    >
      <div className="flex flex-col gap-4 p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            {!isArchived && (
              <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                <FolderIcon className="w-6 h-6 text-blue-600" />
              </div>
            )}
            {isArchived && (
              <div className="w-10 h-10 bg-gray-200 rounded-lg flex items-center justify-center">
                <ArchiveBoxArrowDownIcon className="w-6 h-6 text-gray-600" />
              </div>
            )}
            <div>
              <p className={`text-l font-semibold ${isArchived ? "text-gray-600" : "text-black"}`}>
                <Link
                  href={`/projects/${id}`}
                  passHref
                  className={`${isArchived ? "text-gray-500" : "text-blue-500"}`}
                >
                  {name}
                </Link>
                {isArchived && <span className="text-red-500 text-sm ml-2">(ARCHIVED)</span>}
              </p>
              <p className="text-sm text-gray-500">{formattedCreationTime}</p>
            </div>
          </div>
          {hasAdminAccess && (
            <IconButton
              onClick={handleMenuOpen}
              className="flex items-center justify-center w-8 h-8 hover:bg-gray-100 rounded-full cursor-pointer"
            >
              <EllipsisVerticalIcon className="w-5 h-5" />
            </IconButton>
          )}

          <Menu anchorEl={anchorEl} open={open} onClose={handleMenuClose}>
            {!isArchived &&
            (<>
              <MenuItem onClick={handleEdit} disabled={isArchived}>
                Edit
              </MenuItem>
              <MenuItem onClick={showConfirmArchive} disabled={isArchived}>
                Archive
              </MenuItem>
            </>)}
            <MenuItem onClick={handleExport}>Export</MenuItem>
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
                  className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center"
                >
                  <UserIcon className="w-5 h-5" />
                </div>
              ))}
              {userNames.length > 4 && (
                <div
                  className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium 
    ${isArchived ? "bg-gray-300 text-gray-600" : "bg-blue-100 text-blue-600"}`}
                >
                  +{userNames.length - 4}
                </div>
              )}
            </div>
            <div
              className={`w-16 h-8 rounded-md flex items-center justify-center text-sm font-medium 
    ${isArchived ? "bg-gray-300 text-gray-600" : "bg-blue-100 text-blue-600"}`}
            >
              {assetCount}
            </div>
          </div>
        </div>
      </div>

      <PopupModal
        isOpen={confirmArchivePopup}
        onClose={closeConfirmArchivePopup}
        onConfirm={onConfirmArchive}
        title="Are you sure you want to archive this project?"
        messages={["Warning: This action is irreversible."]}
      />
    </div>
  );
}
