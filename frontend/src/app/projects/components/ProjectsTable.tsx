"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid";
import Pagination from "@mui/material/Pagination";

interface User {
  userId: string;
  name: string;
}

const TempUsers: User[] = [
  { userId: "1", name: "John" },
  { userId: "2", name: "Luke" },
  { userId: "3", name: "Dave" },
];

const TempAssets = [
  {
    id: "1",
    user: TempUsers[0],
    datePosted: "2025-01-26",
    tags: ["construction", "building"],
    thumbnail: "/images/image1.jpg",
    name: "filename",
  },
  {
    id: "2",
    user: TempUsers[1],
    datePosted: "2025-01-29",
    tags: ["office", "building"],
    thumbnail: "/images/image2.jpg",
    name: "filename",
  },
  {
    id: "3",
    user: TempUsers[2],
    datePosted: "2025-02-26",
    tags: ["sports", "soccer", "ball"],
    thumbnail: "/images/image3.jpg",
    name: "filename",
  },
  {
    id: "4",
    user: TempUsers[2],
    datePosted: "2025-02-26",
    tags: [],
    thumbnail: "/images/image4.jpg",
    name: "filename",
  },
  {
    id: "5",
    user: TempUsers[1],
    datePosted: "2025-02-26",
    tags: ["travel", "fast", "usa"],
    thumbnail: "/images/image5.jpg",
    name: "filename",
  },
  {
    id: "6",
    user: TempUsers[2],
    datePosted: "2025-02-26",
    tags: [],
    thumbnail: "/images/image6.jpg",
    name: "filename",
  },
  {
    id: "7",
    user: TempUsers[0],
    datePosted: "2025-02-26",
    tags: [],
    thumbnail: "/images/image7.jpg",
    name: "filename",
  },
];

interface ItemsProps {
  currentItems?: any;
  setCurrentItems?: any;
}

function Items({ currentItems, setCurrentItems }: ItemsProps) {
  return (
    <div className="items min-h-[70vh] overflow-y-auto mt-4 rounded-lg p-4">
      <div className="overflow-x-auto">
        <table className="min-w-full bg-white border border-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Image ID
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Image
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Last Updated
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Posted By
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tags
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Modify
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {currentItems.map((asset: any) => (
              <tr key={asset.id} className="cursor-pointer hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {asset.id}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    <Image
                      src={asset.thumbnail}
                      alt={`${asset.name} thumbnail`}
                      width={120}
                      height={120}
                      className="object-cover rounded w-full h-full"
                    />
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {asset.datePosted}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">{asset.user.name}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex gap-1">
                    {asset.tags.map((tag: any) => (
                      <span
                        key={tag}
                        className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <div className="flex gap-3">
                    <button
                      className="text-indigo-600 hover:text-indigo-900"
                      onClick={(e) => {
                        e.stopPropagation();
                        // EDIT LOGIC
                      }}
                    >
                      <PencilIcon className="h-5 w-5" />
                    </button>
                    <button
                      className="text-red-600 hover:text-red-900"
                      onClick={(e) => {
                        e.stopPropagation();
                        if (
                          confirm(
                            "Are you sure you want to delete this project?"
                          )
                        ) {
                          const updatedImages = currentItems.filter(
                            (img: any) => img.id !== asset.id
                          );
                          setCurrentItems(updatedImages);
                        }
                      }}
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const itemsPerPage = 10;

const ProjectsTable = () => {
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [currentItems, setCurrentItems] = useState<any>([]);

  const [selectedUser, setSelectedUser] = useState<any>("");
  const [selectedDate, setSelectedDate] = useState<any>("");
  // const [selectedStatus, setSelectedStatus] = useState<any>("");
  const [searchTag, setSearchTag] = useState<any>("");

  const [pendingSearchTag, setPendingSearchTag] = useState<any>("");

  // TODO: we need list of users within the project

  // TODO: ADD IMAGE SIZE

  const [users, setUsers] = useState<any>(TempUsers);

  const handleTagBlur = () => {
    setSearchTag(pendingSearchTag);
  };

  const handleChange = async (e: any, page: number) => {
    // await mock api call
    const { items, totalPages } = await fetchAssets(page, {
      selectedUser,
      selectedDate,
      searchTag,
    });
    setCurrentPage(page);
    setTotalPages(totalPages);
    setCurrentItems(items);
  };

  const fetchAssets = async (page: number, filters: {}) => {
    console.log("Fetching logs with filters: ", filters);
    // TODO: await fetch logs
    return {
      items: TempAssets.slice(
        page * itemsPerPage - itemsPerPage,
        page * itemsPerPage
      ),
      totalPages: totalPages,
    };
  };

  useEffect(() => {
    fetchAssets(1, {
      selectedUser,
      selectedDate,
      searchTag,
    }).then(({ items, totalPages }) => {
      setCurrentItems(items);
      setTotalPages(totalPages);
    });
  }, [selectedUser, selectedDate, searchTag]);

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
            {users.map((user: any) => (
              <option key={user.userId} value={user.userId}>
                {user.name}
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
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <input
            type="text"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Search by Tags"
            value={pendingSearchTag}
            onChange={(e) => setPendingSearchTag(e.target.value)}
            onBlur={handleTagBlur}
          />
        </div>
      </div>
      <Items currentItems={currentItems} setCurrentItems={setCurrentItems} />
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

export default ProjectsTable;
