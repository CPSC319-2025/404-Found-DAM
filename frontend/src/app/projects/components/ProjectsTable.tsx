"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import Pagination from "@mui/material/Pagination";
import { fetchWithAuth } from "@/app/utils/api/api";
import { User, Tag, Project, Asset, Pagination as PaginationType } from "@/app/types";

interface ProjectWithTags extends Project {
  tags: Tag[];
}

interface PaginatedAssets {
  assets: Asset[];
  pagination: PaginationType;
}

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
            {currentItems.map((asset: Asset) => (
              <tr key={asset.blobID} className="cursor-pointer hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {asset.blobID}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    <Image
                      src={asset.thumbnailUrl ?? "/images/missing-image.png"}
                      alt={`${asset.filename} thumbnail`}
                      width={120}
                      height={120}
                      className="object-cover rounded w-full h-full"
                    />
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {new Date(asset.date).toLocaleString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">{asset.uploadedBy?.name}</div>
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
                        // TODO: EDIT LOGIC
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
                          // TODO
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

const ProjectsTable = ({ projectID }: { projectID: string }) => {
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [currentItems, setCurrentItems] = useState<Asset[]>([]);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedTag, setSelectedTag] = useState<number>(0);
  const [selectedAssetType, setSelectedAssetType] = useState<string>("");

  // TODO: ADD IMAGE SIZE

  const [users, setUsers] = useState<User[]>([]);
  const [tags, setTags] = useState<Tag[]>([]);

  const fetchAssets = async (page: number) => {
    const queryParams = new URLSearchParams({
      assetsPerPage: String(10),
      pageNumber: String(page),
      postedBy: String(selectedUser),
      tagID: String(selectedTag),
      assetType: selectedAssetType
    }).toString();
    const response = await fetchWithAuth(`projects/${projectID}/assets/pagination?${queryParams}`);

    if (!response.ok) {
      console.error(`Failed to fetch assets (Status: ${response.status} - ${response.statusText})`)
      return { assets: [], totalPages: 0 };
    }

    const data = (await response.json()) as PaginatedAssets;

    return {
      assets: data.assets,
      totalPages: data.pagination.totalPages
    }
  };

  const getProject = async () => {
    const response = await fetchWithAuth(`projects/${projectID}`);

    if (!response.ok) {
      throw new Error("Failed to get project.");
    }

    const project = await response.json();

    if (!project) {
      throw new Error("No project returned from the API.");
    }

    return project as ProjectWithTags;
  }

  const handlePageChange = (e: any, page: number) => {
    setCurrentPage(page);
    fetchAssets(currentPage)
      .then(({ assets, totalPages }) => {
        setCurrentItems(assets);
        setTotalPages(totalPages);
      });
  }

  useEffect(() => {
    // upon filter change we go back to page 1
    setCurrentPage(1);
    fetchAssets(currentPage)
      .then(({ assets, totalPages }) => {
        setCurrentItems(assets);
        setTotalPages(totalPages);
      });
  }, [selectedUser, selectedTag, selectedAssetType]);

  useEffect(() => {
    getProject()
      .then((project: ProjectWithTags) => {
        setUsers(project.admins.concat(project.regularUsers))
        setTags(project.tags)
      })
      .catch((error) => {
        console.error("Error fetching project:", error);
      })
  }, [])

  return (
    <>
      <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedUser}
            onChange={(e) => setSelectedUser(Number(e.target.value))}
          >
            <option value="">Filter by User</option>
            {users.map((user: any) => (
              <option key={user.userID} value={user.userID}>
                {user.name}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedTag}
            onChange={(e) => setSelectedTag(Number(e.target.value))}
          >
            <option value="">Filter by Tag</option>
            {tags.map((tag: Tag) => (
              <option key={tag.tagID} value={tag.tagID}>
                {tag.name}
              </option>
            ))}
          </select>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <select
            className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={selectedAssetType}
            onChange={(e) => setSelectedAssetType(e.target.value)}
          >
            <option value="all">Filter by Asset Type</option>
            <option value="image">
              image
            </option>
            <option value="video">
              video
            </option>
          </select>
        </div>
      </div>
      <Items currentItems={currentItems} setCurrentItems={setCurrentItems} />
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

export default ProjectsTable;