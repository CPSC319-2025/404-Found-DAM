"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { ArrowDownTrayIcon } from "@heroicons/react/24/outline";
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
import { formatFileSize } from "@/app/utils/api/formatFileSize";
import { convertUtcToLocal } from "@/app/utils/api/getLocalTime";
import { getEndOfDayUtc, getStartOfDayUtc } from "@/app/utils/api/localToUtc";
import { getAssetFile } from "@/app/utils/api/getAssetFile";
import { toast } from "react-toastify";
import { downloadAsset } from "@/app/utils/api/getAssetFile";
import { useUser } from "@/app/context/UserContext";
import PopupModal from "@/app/components/ConfirmModal";

interface ProjectWithTags extends Project {
  tags: Tag[];
  name?: string;
}

interface PaginatedAssets {
  assets: Asset[];
  assetIdNameList: { blobID: string, filename: string };
  assetBlobSASUrlList: string[];
  pagination: PaginationType;
}

interface ItemsProps {
  currentItems?: any;
  openPreview: any;
  downloadAssetConfirm: any,
}

interface AssetWithSrc extends Asset {
  src?: string;
}

function Items({ currentItems, openPreview, downloadAssetConfirm }: ItemsProps) {
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
                Uploaded By
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
                className="hover:bg-gray-50"
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
                      alt={`${asset.filename}`}
                      width={120}
                      height={120}
                      className="object-cover rounded w-full h-full cursor-pointer"
                      onClick={() => openPreview(asset)}
                    />
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {formatFileSize(asset.filesizeInKB)}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {convertUtcToLocal(asset.date)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">
                    {asset.uploadedBy?.email}
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
                      onClick={() => downloadAssetConfirm(asset)}
                    >
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                        <ArrowDownTrayIcon className="h-5 w-5" />
                      </span>
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

const downloadAssetWrapper = async (asset: AssetWithSrc, project: any, user: any) => {
  try {
    toast.success("Starting download...");
    await downloadAsset(asset, project, user);
  } catch (e) {
    toast.error((e as Error).message);
  }
}

const ProjectsTable = ({ projectID }: { projectID: string }) => {
  const { user } = useUser();
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [currentItems, setCurrentItems] = useState<AssetWithSrc[]>([]);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedTag, setSelectedTag] = useState<string>("");
  const [selectedAssetType, setSelectedAssetType] = useState<string>("all");
  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");

  const [users, setUsers] = useState<User[]>([]);
  const [tags, setTags] = useState<string[]>([]);

  const [isPreviewAsset, setIsPreviewAsset] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);

  const [projectName, setProjectName] = useState<string>("");
  const [projectDescription, setProjectDescription] = useState<string>("");

  const [confirmDownloadPopup, setConfirmDownloadPopup] = useState(false);
  const [requestedDownloadAsset, setRequestedDownloadAsset] = useState<any>(null);

  const downloadAssetConfirm = async (asset: any) => {
    setRequestedDownloadAsset(asset);
    if (asset.mimetype.includes('image')) {
      setConfirmDownloadPopup(true);
    } else {
      downloadAssetWrapper(false);
    }
  };

  const downloadAssetWrapper = async (addWatermark: boolean) => {
    setConfirmDownloadPopup(false);
    try {
      toast.success("Starting download...");
      await downloadAsset(
        requestedDownloadAsset,
        { projectName, projectID },
        user,
        addWatermark
      );
    } catch (e) {
      toast.error((e as Error).message);
    } finally {
      setRequestedDownloadAsset(null);
    }
  };

  function openPreview(asset: any) {
    if (asset.src) {
      setPreviewUrl(asset.src);
      setPreviewType(asset.mimetype);
      setIsPreviewAsset(true);
    }
  }

  function closeModal() {
    setIsPreviewAsset(false);
    setPreviewUrl(null);
    setPreviewType(null);
  }

  const fetchAssets = async (page: number) => {
    const queryParams = new URLSearchParams({
      assetsPerPage: String(10),
      pageNumber: String(page),
      postedBy: String(selectedUser),
      tagName: String(selectedTag),
      assetType: selectedAssetType,
    });

    if (startDate) {
      queryParams.append("fromDate", getStartOfDayUtc(startDate));
    }

    if (endDate) {
      queryParams.append("toDate", getEndOfDayUtc(endDate));
    }

    const url = `projects/${projectID}/assets/pagination?${queryParams.toString()}`;
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
      assetBlobSASUrlList: data.assetBlobSASUrlList,
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

  const getTags = async () => {
    const response = await fetchWithAuth("/tags");

    if (!response.ok) {
      throw new Error("Failed to get tags.");
    }

    const tags = await response.json();

    if (!tags) {
      throw new Error("No tags returned from the API.");
    }

    return tags as string[];
  }

  const setAssetSrcs = (assets: AssetWithSrc[], assetBlobSASUrlList: string[]) => {
    assets.forEach(async (asset: AssetWithSrc, index: number) => {
      try {
        const src = (await getAssetFile(assetBlobSASUrlList[index], asset.mimetype || "")) as string;
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
    setCurrentPage(page);
    fetchAssets(page).then(({ assets, assetBlobSASUrlList, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets, assetBlobSASUrlList! as string[]);
    });
  };

  useEffect(() => {
    // upon filter change we go back to page 1
    setCurrentPage(1);
    fetchAssets(1).then(({ assets, assetBlobSASUrlList, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets, assetBlobSASUrlList! as string[]);
    });
  }, [selectedUser, selectedTag, selectedAssetType, startDate, endDate]);

  useEffect(() => {
    getProject()
      .then((project: ProjectWithTags) => {
        setUsers(project.admins.concat(project.regularUsers));
        setProjectName(project.name!);
        setProjectDescription(project.description);
      })
      .catch((error) => {
        console.error("Error fetching project:", error);
      });
    getTags()
      .then((tags: any) => {
        setTags(tags);
      })
  }, []);

  return (
    <>
      <h1 className="text-2xl font-bold mb-4">
        {"Project: " + projectName}
      </h1>
      <h6 className="mb-4">
        {"Description: " + projectDescription}
      </h6>
      <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by User</label>
          <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
            <select
              className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={selectedUser}
              onChange={(e) => setSelectedUser(Number(e.target.value))}
            >
              <option value="">Select User</option>
              {users.map((user: any) => (
                <option key={user.userID} value={user.userID}>
                  {user.name} ({user.email})
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by Tag</label>
          <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
            <select
              className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={selectedTag}
              onChange={(e) => setSelectedTag(String(e.target.value))}
            >
              <option value="">Select Tag</option>
              {tags.map((tag: string) => (
                <option key={tag} value={tag}>
                  {tag}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">Filter by Asset Type</label>
          <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
            <select
              className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={selectedAssetType}
              onChange={(e) => setSelectedAssetType(e.target.value)}
            >
              <option value="all">Select Asset Type</option>
              <option value="image">image</option>
              <option value="video">video</option>
            </select>
          </div>
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
      <Items currentItems={currentItems} openPreview={openPreview} downloadAssetConfirm={downloadAssetConfirm} />
      <Pagination
        count={totalPages}
        page={currentPage}
        onChange={handlePageChange}
        shape="rounded"
        color="standard"
        className="border border-gray-300"
      />

      {confirmDownloadPopup && (
        <div onClick={(e) => e.stopPropagation()}>
          <PopupModal
            title="Would you like to add a watermark to the image?"
            isOpen={true}
            onClose={() => downloadAssetWrapper(false)}
            onConfirm={() => downloadAssetWrapper(true)}
            messages={[]}
          />
        </div>
      )}

      {isPreviewAsset && previewUrl && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="relative bg-white p-4 rounded shadow-lg max-w-3xl max-h-[80vh] overflow-auto">
            <button
              onClick={closeModal}
              className="absolute top-2 right-2 text-gray-500 hover:text-gray-700"
            >
              ✕
            </button>

            {previewType?.startsWith("image/") && (
              <img
                src={previewUrl}
                alt="Full Preview"
                className="max-w-full max-h-[70vh]"
              />
            )}
            {previewType?.startsWith("video/") && (
              <video
                src={previewUrl}
                controls
                className="max-w-full max-h-[70vh]"
              />
            )}
          </div>
        </div>
      )}
    </>
  );
};

export default ProjectsTable;
