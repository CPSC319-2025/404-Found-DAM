"use client";

import React, { useState, useEffect } from "react";
import {
  PencilIcon,
  ArrowUpIcon,
  ArrowDownIcon,
  PlusIcon,
  DownloadIcon,
  ArrowUpOnSquareIcon,
  TrashIcon,
  ArchiveBoxArrowDownIcon,
} from '@heroicons/react/24/solid';
import Pagination from "@mui/material/Pagination";
import { User, Project, Asset, Log, Pagination as PaginationType } from "@/app/types";
import { fetchWithAuth } from "@/app/utils/api/api";
import { convertUtcToLocal } from "@/app/utils/api/getLocalTime";

interface PaginatedLogs extends PaginationType {
  data: Log[];
}

interface ItemsProps {
  currentItems?: Log[];
}

const changeTypes = [
  { value: "Create", icon: <PlusIcon className="w-8 h-8 text-blue-400"/> },
  { value: "Updated" icon: <PencilIcon className="w-8 h-8 text-blue-400"/> },
  { value: "Uploaded" icon: <ArrowUpOnSquareIcon className="w-8 h-8 text-blue-400"},
  { value: "Added" icon: <PlusIcon className="w-8 h-8 text-blue-400"},
  { value: "Import" icon: <PlusIcon className="w-8 h-8 text-blue-400"},
  { value: "Export" icon: <ArrowUpIcon className="w-8 h-8 text-blue-400"},
  { value: "Archived" icon: <ArchiveBoxArrownDownIcon className="w-8 h-8 text-blue-400"},
  { value: "Deleted" icon: <TrashIcon className="w-8 h-8 text-blue-400"},
  { value: "Downloaded" icon: <DownloadIcon className="w-8 h-8 text-blue-400"},
  { value: "Other" icon: <ArrowDownIcon className="w-8 h-8 text-blue-400"},
]

function getIconForChangeType(changeType: string) {
  switch (changeType) {
    case 'Create':
      return <PlusIcon className="w-8 h-8 text-blue-400" />;
    case 'Updated':
      return <PencilIcon className="w-8 h-8 text-blue-400"/>;
    case 'Uploaded':
      return <ArrowUpOnSquareIcon className="w-8 h-8 text-blue-400" />;
    case 'Added':
      return <PlusIcon className="w-8 h-8 text-blue-400" />;
    case 'Import':
      return <PlusIcon className="w-8 h-8 text-blue-400" />;
    case 'Export':
      return <ArrowUpIcon className="w-8 h-8 text-blue-400" />;
    case 'Archived':
      return <ArchiveBoxArrowDownIcon className="w-8 h-8 text-blue-400" />;
    case 'Deleted':
      return <TrashIcon className="w-8 h-8 text-blue-400" />;
    case 'Downloaded':
      return <DownloadIcon className="w-8 h-8 text-blue-400" />;
    default:
      return <ArrowDownIcon className="w-8 h-8 text-blue-400" />;
  }
}

function Items({ currentItems }: ItemsProps) {
  return (
    <div className="grid bg-gray-50 overflow-y-auto mt-4 p-4">
      {currentItems && currentItems.map((log: Log) => {
        const IconComponent = getIconForChangeType(log.change_type);

        return (
          <div
            key={log.change_id}
            className="flex items-center p-2 m-1 border-b border-gray-200 last:border-0"
          >
            <div className="mr-3">
              {IconComponent}
            </div>
            <div className="flex flex-col items-start p-2">
              {log.description}
              <span className="text-gray-400 text-sm">
                {convertUtcToLocal(log.date_time)}
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}

const LogsTable = () => {
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [currentItems, setCurrentItems] = useState<Log[]>([]);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedProject, setSelectedProject] = useState<number>(0);
  const [selectedChangeType, setSelectedChangeType] = useState<string>("");
  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");

  const [users, setUsers] = useState<User[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);

  const getLogs = async (page: number) => {
    const queryParams = new URLSearchParams({
      pageSize: String(10),
      pageNumber: String(page)
    })

    if (startDate && endDate) {
      queryParams.append("start", startDate);
      queryParams.append("end", endDate);
    }

    if (selectedUser !== 0) {
      queryParams.append("userID", String(selectedUser));
    }
    if (selectedProject !== 0) {
      queryParams.append("projectID", String(selectedProject));
    }

    if (selectedChangeType !== "") {
      queryParams.append("changeType", String(selectedProject));
    }

    const response = await fetchWithAuth(
      `logs?${queryParams.toString()}`
    );

    if (!response.ok) {
      console.error(
        `Failed to fetch assets (Status: ${response.status} - ${response.statusText})`
      );
      return { logs: [], totalPages: 0 };
    }

    const data = (await response.json()) as PaginatedLogs;

    return {
      logs: data.data,
      totalPages: data.totalPages,
    };
  };

  const getProjects = async () => {
    const response = await fetchWithAuth("projects");

    if (!response.ok) {
      throw new Error("Failed to fetch projects");
    }

    const data = await response.json();

    return data.fullProjectInfos as Project[];
  }

  const getUsers = async () => {
    const response = await fetchWithAuth("users");

    if (!response.ok) {
      throw new Error("Failed to fetch users");
    }

    const data = await response.json();

    return data.users as User[];
  }

  const handlePageChange = (e: any, page: number) => {
    setCurrentPage(page);
    getAllData(page);
  };

  useEffect(() => {
    setCurrentPage(1);
    getAllData(1);
  }, [selectedUser, selectedProject, selectedChangeType, startDate, endDate]);

  const getAllData = async (page: number) => {
    const projects = await getProjects();
    setProjects(projects);
    const users = await getUsers();

    const verboseUsers = users.map((user) => ({
      ...user,
      name: `${user.name} (${user.email})`,
    }));
    setUsers(verboseUsers);

    const { logs, totalPages } = await getLogs(page);

    setCurrentItems(logs);
    setTotalPages(totalPages);
  }

  return (
    <>
      <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by User</label>
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedUser}
            onChange={(e) => setSelectedUser(Number(e.target.value))}
          >
            <option value="">Select User</option>
            {users.map((user: User) => (
              <option key={user.userID} value={user.userID}>
                {user.name} ({user.email})
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by Project</label>
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedProject}
            onChange={(e) => setSelectedProject(Number(e.target.value))}
          >
            <option value="">Select Project</option>
            {projects.map((project: Project) => (
              <option key={project.projectID} value={project.projectID}>
                {project.projectName} (ID: {project.projectID})
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by Log Type</label>
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedProject}
            onChange={(e) => setSelectedProject(String(e.target.value))}
          >
            <option value="">Select Log Type</option>
            {changeTypes.map((changeType: string) => (
              <option key={changeType} value={changeType}>
                {changeType}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Start Date</label>
          <input
            type="date"
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </div>

        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">End Date</label>
          <input
            type="date"
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
        </div>
      </div>
      <Items currentItems={currentItems} />
      <Pagination
        count={totalPages}
        page={currentPage}
        onChange={handlePageChange}
        shape="rounded"
        color="standard"
        className="border border-gray-300"
      />
    </>
  );
};

export default LogsTable;
