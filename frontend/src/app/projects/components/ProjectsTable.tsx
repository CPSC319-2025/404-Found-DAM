"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import Pagination from "@mui/material/Pagination";
import { fetchWithAuth } from "@/app/utils/api/api";
import {
  User,
  Tag,
  Project,
  Asset,
  Pagination as PaginationType,
} from "@/app/types";
import { ZstdCodec } from "zstd-codec";

interface ProjectWithTags extends Project {
  tags: Tag[];
}

interface PaginatedAssets {
  assets: Asset[];
  assetIdNameList: { blobID: string, filename: string };
  pagination: PaginationType;
}

interface ItemsProps {
  currentItems?: any;
  setCurrentItems?: any;
  projectID: any;
}

interface AssetWithSrc extends Asset {
  src?: string;
}

function formatFileSize(sizeInKB: number) {
  if (sizeInKB >= 1024 * 1024) {
    return (sizeInKB / (1024 * 1024)).toFixed(2) + " GB";
  } else if (sizeInKB >= 1024) {
    return (sizeInKB / 1024).toFixed(2) + " MB";
  } else {
    return sizeInKB.toFixed(2) + " KB";
  }
}

function Items({ currentItems, setCurrentItems, projectID }: ItemsProps) {
  return (
    <div className="items min-h-[70vh] overflow-y-auto mt-4 rounded-lg p-4">
      <div className="overflow-x-auto">
        <table className="min-w-full bg-white border border-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                File Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Image
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Filesize
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
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {currentItems.map((asset: AssetWithSrc) => (
              <tr
                key={asset.blobID}
                className="cursor-pointer hover:bg-gray-50"
              >
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {asset.filename}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    <Image
                      src={asset.src ?? ""}
                      alt={`${asset.filename} thumbnail`}
                      width={120}
                      height={120}
                      className="object-cover rounded w-full h-full"
                    />
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {formatFileSize(asset.filesizeInKB)}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {new Date(asset.date).toLocaleString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">
                    {asset.uploadedBy?.name}
                  </div>
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
                    {/*<button*/}
                    {/*  className="text-red-600 hover:text-red-900"*/}
                    {/*  onClick={(e) => {*/}
                    {/*    e.stopPropagation();*/}
                    {/*    if (*/}
                    {/*      confirm(*/}
                    {/*        "Are you sure you want to delete this asset?"*/}
                    {/*      )*/}
                    {/*    ) {*/}
                    {/*      // TODO*/}
                    {/*    }*/}
                    {/*  }}*/}
                    {/*>*/}
                    {/*  <TrashIcon className="h-5 w-5" />*/}
                    {/*</button>*/}
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
  const [currentItems, setCurrentItems] = useState<AssetWithSrc[]>([]);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedTag, setSelectedTag] = useState<number>(0);
  const [selectedAssetType, setSelectedAssetType] = useState<string>("all");

  // TODO: ADD IMAGE SIZE

  const [users, setUsers] = useState<User[]>([]);
  const [tags, setTags] = useState<Tag[]>([]);

  const getAssetFile = async (blobID: string, filename: string) => {
    // if (!filename.includes(".webp")) {
    //   // TODO: handle video
    //   return Promise.reject("Unable to handle video yet")
    // }
    const response = await fetchWithAuth(`project/${projectID}/asset-files/storage/${blobID}/${filename}`);

    if (!response.ok) {
      throw new Error(`Fetch failed with status ${response.status}`);
    }

    const blob = await response.blob();
    const contentType = response.headers.get("content-type");

    console.log("Content type:", contentType);

    const fileContent = new Uint8Array(await blob.arrayBuffer());

    return new Promise((resolve, reject) => {
      ZstdCodec.run((zstd: any) => {
        try {
          const simple = new zstd.Simple();
          const decompressed = simple.decompress(fileContent);

          const decompressedBlob = new Blob([decompressed], { type: contentType || "image/webp" });

          const url = URL.createObjectURL(decompressedBlob);
          resolve(url);
        } catch (error) {
          reject(error);
        }
      });
    });
  };

  const fetchAssets = async (page: number) => {
    const queryParams = new URLSearchParams({
      assetsPerPage: String(10),
      pageNumber: String(page),
      postedBy: String(selectedUser),
      tagID: String(selectedTag),
      assetType: selectedAssetType,
    }).toString();
    const url = `projects/${projectID}/assets/pagination?${queryParams}`;
    const response = await fetchWithAuth(url);

    if (!response.ok) {
      console.error(
        `Failed to fetch assets (Status: ${response.status} - ${response.statusText})`
      );
      return { assets: [], totalPages: 0 };
    }

    const data = (await response.json()) as PaginatedAssets;

    return {
      assets: data.assets,
      totalPages: data.pagination.totalPages,
    };
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
  };

  const setAssetSrcs = (assets: AssetWithSrc[]) => {
    assets.forEach(async (asset: AssetWithSrc) => {
      try {
        const src = (await getAssetFile(asset.blobID, asset.filename)) as string;
        setCurrentItems((prevItems: AssetWithSrc[]) =>
          prevItems.map((item: AssetWithSrc) =>
            item.blobID === asset.blobID ? { ...item, src } : item
          )
        );
      } catch (error) {
        console.error(`Error loading asset ${asset.blobID}:`, error);
      }
    });
  }

  const handlePageChange = (e: any, page: number) => {
    console.log("setting page to: ", page)
    setCurrentPage(page);
    fetchAssets(page).then(({ assets, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets);
    });
  };

  useEffect(() => {
    // upon filter change we go back to page 1
    setCurrentPage(1);
    fetchAssets(1).then(({ assets, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets);
    });
  }, [selectedUser, selectedTag, selectedAssetType]);

  useEffect(() => {
    getProject()
      .then((project: ProjectWithTags) => {
        setUsers(project.admins.concat(project.regularUsers));
        setTags(project.tags);
      })
      .catch((error) => {
        console.error("Error fetching project:", error);
      });
  }, []);

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
                {user.name} ({user.email})
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
            <option value="image">image</option>
            <option value="video">video</option>
          </select>
        </div>
      </div>
      <Items currentItems={currentItems} setCurrentItems={setCurrentItems} projectID={projectID} />
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