"use client";

import React, { useState, useEffect } from "react";
// import Image from "next/image";
// import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
// import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid";
import Pagination from '@mui/material/Pagination';

interface Log {
  id: string;
  user: { userId: string; name: string };
  asset?: { blobId: string, filename: string };
  project: { projectId: string, name: string };
  action: string;
  timestamp: string;
}

const TempLogs: Log[] = [
  {
    id: "1",
    user: {
      userId: "1",
      name: "John",
    },
    asset: {
      blobId: "1",
      filename: "file1.jpg"
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
    user: {
      userId: "1",
      name: "John",
    },
    asset: {
      blobId: "1",
      filename: "file1.jpg"
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
    user: {
      userId: "1",
      name: "John",
    },
    asset: {
      blobId: "1",
      filename: "file1.jpg"
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
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "3",
      name: "Admin Aaron",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "10",
    user: {
      userId: "3",
      name: "Admin Aaron",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
  {
    id: "11",
    user: {
      userId: "2",
      name: "Luke",
    },
    asset: {
      blobId: "2",
      filename: "file1.jpg"
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
    user: {
      userId: "3",
      name: "Admin Aaron",
    },
    project: {
      projectId: "1",
      name: "Project 1",
    },
    action: "Modified tags",
    timestamp: "2011-10-07T11:48:00.000Z",
  },
];

function Items({ currentItems }) {
  return (
    <div className="items min-h-[775px] overflow-y-auto">
      {currentItems && currentItems.map((log) => (
        <div key={log.id} className="flex border p-3 rounded-md shadow-sm mb-2">
          <div className="flex-1 p-2">
            <h3 className="text-lg font-semibold">Log ID: {log.id}</h3>
          </div>
          <div className="flex-1 p-2">
            <p><strong>User:</strong> {log.user.name} </p>
          </div>
          <div className="flex-1 p-2">
            {log.asset && <p><strong>Asset:</strong> {log.asset.filename} </p> }
          </div>
          <div className="flex-1 p-2">
            <p><strong>Project:</strong> {log.project?.name} </p>
          </div>
          <div className="flex-1 p-2">
            <p><strong>Action:</strong> {log.action} </p>
          </div>
          <div className="flex-1 p-2">
            <p><strong>Timestamp:</strong> {new Date(log.timestamp).toLocaleString()} </p>
          </div>
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
  const [filters, setFilters] = useState([]);

  const handleChange = async (e: React.ChangeEvent<HTMLInputElement>, page: number) => {
    // await mock api call
    // order by lastmodified
    const { items, totalPages } = await fetchLogs(page, []);
    setCurrentPage(page);
    setTotalPages(totalPages);
    setCurrentItems(items);
  }

  const fetchLogs = async (page: number, filters: any) => {
    console.log("Fetching logs");
    // TODO: await fetch logs
    return {
      items: TempLogs.slice(page * itemsPerPage - itemsPerPage, page * itemsPerPage),
      totalPages: totalPages,
    }
  }

  useEffect(() => {
    fetchLogs(currentPage, []).then(({ items, totalPages }) => {
      setCurrentItems(items);
      setTotalPages(totalPages);
    });
  }, [filters]);

  return (
    <>
      <div>FILTERS TODO</div>
      <Items currentItems={currentItems} />
      <Pagination
        count={totalPages}
        page={currentPage}
        onChange={handleChange}
        shape="rounded"
        color="standard"
      />
    </>
  )
};

export default LogsTable;