"use client";

import React, { useState, useEffect } from "react";
// import Image from "next/image";
// import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
// import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid";
import Pagination from "@mui/material/Pagination";

interface Log {
  id: string;
  user: { userId: string; name: string };
  asset?: { blobId: string; filename: string };
  project: { projectId: string; name: string };
  action: string;
  timestamp: string;
}

interface User {
  userId: string;
  name: string;
}

const TempUsers: User[] = [
  { userId: "1", name: "John" },
  { userId: "2", name: "Luke" },
  { userId: "3", name: "Admin Aaron" },
];

const TempProjects = [
  { projectId: "1", name: "Project 1" },
  { projectId: "2", name: "Project 2" },
];

const TempAssets = [
  { blobId: "1", filename: "Asset1.png" },
  { blobId: "2", filename: "Asset2.png" },
];

const TempLogs: Log[] = [
  {
    id: "1",
    user: TempUsers[0],
    asset: {
      blobId: "1",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Uploaded",
    timestamp: "2011-10-05T14:48:00.000Z",
  },
  {
    id: "2",
    user: TempUsers[0],
    asset: {
      blobId: "1",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Uploaded",
    timestamp: "2011-10-05T14:48:00.000Z",
  },
  {
    id: "3",
    user: TempUsers[0],
    asset: {
      blobId: "1",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Uploaded",
    timestamp: "2011-10-05T14:48:00.000Z",
  },
  {
    id: "4",
    user: TempUsers[1],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "5",
    user: TempUsers[1],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "6",
    user: TempUsers[1],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "7",
    user: TempUsers[1],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "8",
    user: TempUsers[1],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "9",
    user: TempUsers[2],
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "10",
    user: TempUsers[2],
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "11",
    user: TempUsers[2],
    asset: {
      blobId: "2",
      filename: "file1.jpg",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Downloaded",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "12",
    user: TempUsers[2],
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
];

interface ItemsProps {
  currentItems?: Log[];
}

function Items({ currentItems } : ItemsProps) {
  return (
    <div className="items min-h-[90vh] overflow-y-auto mt-4 rounded-lg p-4">
      {currentItems &&
        currentItems.map((log: any) => (
          <div
            key={log.id}
            className="p-2 mb-2 bg-white rounded-md shadow-sm border border-gray-200"
          >
            <p className="text-gray-800">
              <strong>{log.user?.name || "Unknown User"}</strong> {log.action}{" "}
              {log.project?.name ? `on ${log.project.name}` : ""}
              {log.asset ? ` (Asset: ${log.asset.filename})` : ""} -{" "}
              <span className="text-gray-500 text-sm">
                {new Date(log.timestamp).toLocaleString()}
              </span>
            </p>
          </div>
        ))}
    </div>
  );
}

const itemsPerPage = 10;

const LogsTable = () => {
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(4);
  const [currentItems, setCurrentItems] = useState<Log[]>([]);

  const [selectedUser, setSelectedUser] = useState<any>("");
  const [selectedAsset, setSelectedAsset] = useState<any>("");
  const [selectedProject, setSelectedProject] = useState<any>("");
  const [selectedDate, setSelectedDate] = useState<any>("");

  // TODO: we need list of users with a log entry that we should be able to see
  // TODO: we need list of all assets with a log entry that a user should be able to see
  // TODO: we need list of all projects with a log entry that a user should be able to see

  const [users, setUsers] = useState<User[]>(TempUsers);
  const [assets, setAssets] = useState<any>(TempAssets);
  const [projects, setProjects] = useState<any>(TempProjects);

  const handleChange = async (e: any, page: number) => {
    // await mock api call
    // order by lastmodified
    const { items, totalPages } = await fetchLogs(page, {
      selectedUser,
      selectedAsset,
      selectedProject,
      selectedDate,
    });
    setCurrentPage(page);
    setTotalPages(totalPages);
    setCurrentItems(items);
  };

  const fetchLogs = async (page: number, filters: {}) => {
    console.log("Fetching logs with filters: ", filters);
    // TODO: await fetch logs
    return {
      items: TempLogs.slice(
        page * itemsPerPage - itemsPerPage,
        page * itemsPerPage,
      ),
      totalPages: totalPages,
    };
  };

  useEffect(() => {
    fetchLogs(1, {
      selectedUser,
      selectedAsset,
      selectedProject,
      selectedDate,
    }).then(({ items, totalPages }) => {
      setCurrentItems(items);
      setTotalPages(totalPages);
    });
  }, [selectedUser, selectedAsset, selectedProject, selectedDate]);

  return (
    <>
      <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedUser}
            onChange={(e) => setSelectedUser(e.target.value)}
          >
            <option value="">Filter by User</option>
            {users.map((user) => (
              <option key={user.userId} value={user.userId}>
                {user.name}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedUser}
            onChange={(e) => setSelectedUser(e.target.value)}
          >
            <option value="">Filter by Asset</option>
            {assets.map((asset: any) => (
              <option key={asset.blobId} value={asset.blobId}>
                {asset.filename}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedUser}
            onChange={(e) => setSelectedUser(e.target.value)}
          >
            <option value="">Filter by Project</option>
            {projects.map((project: any) => (
              <option key={project.projectId} value={project.name}>
                {project.name}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <input
            type="date"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Filter by Date"
            value={selectedDate}
            onChange={(e) => setSelectedDate(e.target.value)}
          />
        </div>
      </div>
      <Items currentItems={currentItems} />
      <Pagination
        count={totalPages}
        page={currentPage}
        onChange={handleChange}
        shape="rounded"
        color="standard"
        className="border border-gray-300"
      />
    </>
  );
};

export default LogsTable;
