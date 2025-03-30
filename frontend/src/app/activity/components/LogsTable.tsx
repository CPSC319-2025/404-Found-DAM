"use client";

import React, { useState, useEffect } from "react";
// import Image from "next/image";
// import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
// import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid";
import {
  ArrowUpOnSquareIcon,
  ArrowDownOnSquareIcon,
  PencilSquareIcon,
} from "@heroicons/react/24/solid";
import Pagination from "@mui/material/Pagination";
import { User, Project, Asset, Log, Pagination as PaginationType } from "@/app/types";
import { fetchWithAuth } from "@/app/utils/api/api";

interface User {
  userId: string;
  name: string;
}

interface PaginatedLogs extends PaginationType {
  data: Log[];
}

interface LogVerbose extends Log {
  userInfo: User;
  project: Project;
}

interface ItemsProps {
  currentItems?: Log[];
}

function Items({ currentItems }: ItemsProps) {
  return (
    <div className="grid bg-gray-50 overflow-y-auto mt-4 p-4">
      {currentItems &&
        currentItems.map((log: LogVerbose) => {
          let IconComponent;
          if (log.change_type === "Uploaded") {
            IconComponent = ArrowUpOnSquareIcon;
          } else if (log.change_type === "Downloaded") {
            IconComponent = ArrowUpOnSquareIcon;
          } else if (log.change_type === "Modified tags") {
            IconComponent = PencilSquareIcon;
          } else {
            IconComponent = ArrowUpOnSquareIcon;
          }

          const isAdmin =
            log.userInfo?.name.toLowerCase().includes("admin") || false;
          const userNameClass = isAdmin ? "text-blue-500" : "text-gray-700";

          let renderedText;
          if (log.change_type === "Downloaded") {
            renderedText = (
              <p className="text-gray-800">
                <strong className={userNameClass}>
                  {log.userInfo?.name || "Unknown User"}
                </strong>{" "}
                Downloaded{" "}
                {log.asset_id && (
                  <span className="font-semibold">{log.asset.filename}</span>
                )}{" "}
                from{" "}
                {log.project?.projectName && (
                  <span className="font-semibold text-blue-500">
                    {log.project.projectName}
                  </span>
                )}
              </p>
            );
          } else if (log.change_type === "Uploaded") {
            renderedText = (
              <p className="text-gray-800">
                <strong className={userNameClass}>
                  {log.userInfo?.name || "Unknown User"}
                </strong>{" "}
                Uploaded{" "}
                {log.asset_id && (
                  <span className="font-semibold">{log.asset.filename}</span>
                )}{" "}
                to{" "}
                {log.project?.projectName && (
                  <span className="font-semibold text-blue-500">
                    {log.project.projectName}
                  </span>
                )}
              </p>
            );
          } else if (log.change_type === "Modified tags") {
            renderedText = (
              <p className="text-gray-800">
                <strong className={userNameClass}>
                  {log.userInfo?.name || "Unknown User"}
                </strong>{" "}
                Modified Tags in{" "}
                {log.project?.projectName && (
                  <span className="font-semibold text-blue-500">
                    {log.project.projectName}
                  </span>
                )}
              </p>
            );
          } else {
            renderedText = (
              <p className="text-gray-800">
                <strong className={userNameClass}>
                  {log.userInfo?.name || "Unknown User"}
                </strong>{" "}
                {log.change_type}{" "}
                {log.project?.projectName && (
                  <span className="text-blue-500">{log.project.projectName}</span>
                )}
              </p>
            );
          }

          return (
            <div
              key={log.change_id}
              className="flex items-center p-2 m-1 border-b border-gray-200 last:border-0"
            >
              <div className="mr-3">
                <IconComponent className="w-8 h-8 text-blue-400" />
              </div>
              <div className="flex flex-col items-start p-2">
                {renderedText}
                <span className="text-gray-400 text-sm">
                  {new Date(log.timestamp).toLocaleString()}
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
  const [currentItems, setCurrentItems] = useState<LogVerbose[]>([]);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedProject, setSelectedProject] = useState<number>(0);
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
  }, [selectedUser, selectedProject, startDate, endDate]);

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
    const verboseLogs: LogVerbose[] = logs.map((log) => ({
      ...log,
      userInfo: users.find((user) => user.userID === log.user) || null,
      project: projects.find((project) => project.projectID === log.project_id) || null,
    }));

    setCurrentItems(verboseLogs);
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
                {project.projectName} ({project.projectID})
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