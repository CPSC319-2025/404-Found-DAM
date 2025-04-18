"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { ArrowDownTrayIcon, TrashIcon, DocumentTextIcon } from "@heroicons/react/24/outline";
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
import GenericForm from "@/app/components/GenericForm";
import LoadingSpinner from "@/app/components/LoadingSpinner";

interface ProjectWithTags extends Project {
  tags: Tag[];
  name?: string;
}

interface PaginatedAssets {
  assets: Asset[];
  assetIdNameList: { blobID: string; filename: string };
  assetBlobSASUrlList: string[];
  pagination: PaginationType;
}

interface ItemsProps {
  currentItems?: any;
  openPreview: any;
  downloadAssetConfirm: any;
  deleteAsset: (asset: AssetWithSrc) => void;
  isAdmin: boolean;
  showAssetMetadata: any;
}

interface AssetWithSrc extends Asset {
  src?: string;
}

function Items({
  currentItems,
  openPreview,
  downloadAssetConfirm,
  deleteAsset,
  isAdmin,
  showAssetMetadata,
}: ItemsProps) {
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
              <tr key={asset.blobID} className="hover:bg-gray-50"> 
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {asset.filename}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    {!asset.src && (
                      <div className="h-full w-full flex items-center justify-center bg-gray-100 animate-pulse" style={{ animationDuration: '0.7s' }}>
                        <span className="text-gray-400 text-xs">Loading</span>
                      </div>
                    )}
                    {asset.src && asset.mimetype!.includes("image") && (
                      <Image
                        src={asset.src}
                        alt={`${asset.filename}`}
                        width={120}
                        height={120}
                        className="object-cover rounded w-full h-full cursor-pointer"
                        onClick={() => openPreview(asset)}
                      />
                    )}
                    {asset.src && !asset.mimetype!.includes("image") && (
                      <video
                        src={asset.src ?? ""}
                        width={120}
                        height={120}
                        className="object-cover rounded w-full h-full cursor-pointer"
                        onClick={() => openPreview(asset)}
                      />
                    )}
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
                      onClick={() => showAssetMetadata(asset)}
                    >
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                        <DocumentTextIcon className="h-5 w-5" />
                      </span>
                    </button>
                    <button
                      className="text-indigo-600 hover:text-indigo-900"
                      onClick={() => downloadAssetConfirm(asset)}
                    >
                      <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                        <ArrowDownTrayIcon className="h-5 w-5" />
                      </span>
                    </button>
                    {isAdmin && (
                      <button
                        className="text-red-600 hover:text-red-900"
                        onClick={() => deleteAsset(asset)}
                      >
                        <span className="flex items-center justify-center w-8 h-8 rounded-full bg-gray-200 hover:bg-gray-300 transition">
                          <TrashIcon className="h-5 w-5" />
                        </span>
                      </button>
                    )}
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

const downloadAssetWrapper = async (
  asset: AssetWithSrc,
  project: any,
  user: any
) => {
  try {
    toast.success("Starting download...");
    await downloadAsset(asset, project, user);
  } catch (e) {
    toast.error((e as Error).message);
  }
};

const ProjectsTable = ({ projectID }: { projectID: string }) => {
  const { user } = useUser();
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [currentItems, setCurrentItems] = useState<AssetWithSrc[]>([]);

  const [isAdmin, setIsAdmin] = useState(false);

  const [selectedUser, setSelectedUser] = useState<number>(0);
  const [selectedTag, setSelectedTag] = useState<string>("");
  const [selectedAssetType, setSelectedAssetType] = useState<string>("all");
  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");

  const [withinOneProjectSearchQuery, setWithinOneProjectSearchQuery] = useState<string>("");
  const [isLoading, setIsLoading] = useState(false);
  // const [currentAssets, setCurrentAssets] = useState<any[]>([]);
  // const [searchDone, setSearchDone] = useState<boolean>(false);

  const [users, setUsers] = useState<User[]>([]);
  const [tags, setTags] = useState<string[]>([]);

  const [isPreviewAsset, setIsPreviewAsset] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [previewType, setPreviewType] = useState<string | null>(null);

  const [projectName, setProjectName] = useState<string>("");
  const [projectDescription, setProjectDescription] = useState<string>("");

  const [confirmDownloadPopup, setConfirmDownloadPopup] = useState(false);
  const [requestedDownloadAsset, setRequestedDownloadAsset] =
    useState<any>(null);

  const [isPreviewAssetMetadata, setIsPreviewAssetMetadata] = useState(false);
  const [assetMetadataFields, setAssetMetadataFields] = useState<any[]>([]);
  const [projectMetadataFields, setProjectMetadataFields] = useState<any[]>([]);
  const [assetMetadataName, setAssetMetadataName] = useState<string>("");

  const showAssetMetadata = async (asset: any) => {
    const response = await fetchWithAuth(`palette/blob/${asset.blobID}/fields`);
    if (response.status === 410 || response.status === 404) { // please note: the above fetchWithAuth call returns 404 if the asset has been deleted.
      const errorData = await response.json();
      toast.error("Asset has been deleted. Refreshing...");
      setTimeout(() => {
        window.location.reload();
      }, 1500);
      return;
    }
    if (!response.ok) {
      console.error(
        `Failed to fetch asset metadata (Status: ${response.status} - ${response.statusText})`
      );
      return;
    }

    const data = await response.json();

    const metadataFields = data.fields
      .filter((field: any) => {
        const foundField = projectMetadataFields.find((pmf: any) => pmf.fieldID === field.fieldId);
        if (foundField && foundField.isEnabled) {
          return true;
        } else {
          return false;
        }
      })
      .map((field: any) => {
        return {
          name: field.fieldName,
          label: field.fieldName,
          type: field.fieldType,
          placeholder: "",
          value: field.fieldValue
        }
      });

    if (metadataFields.length < 1) {
      toast.warn("No custom metadata associated to this asset");
      return;
    }

    setAssetMetadataFields(metadataFields);

    setAssetMetadataName(asset.filename);

    setIsPreviewAssetMetadata(true);
  }

  const [confirmDeletePopup, setConfirmDeletePopup] = useState(false);
  const [assetToDelete, setAssetToDelete] = useState<AssetWithSrc | null>(null);

  const downloadAssetConfirm = async (asset: any) => {
    if (asset.mimetype.includes("image")) {
      setRequestedDownloadAsset(asset);
      setConfirmDownloadPopup(true);
    } else {
      downloadAssetWrapper(false, asset);
    }
  };

  const downloadAssetWrapper = async (addWatermark: boolean, asset: any) => {
    setConfirmDownloadPopup(false);
    try {

      // Sean:
      // Call endpoint to check if file still exists before downloading. If not, throw new Error
      // console.log("projID: " + projectID)
      // console.log(asset.blobID);
      try {
        const checkResponse = await fetchWithAuth(`/projects/${projectID}/${asset.blobID}`, {
          method: "GET",
        });

        if (!checkResponse.ok) {

          toast.error("Asset has been deleted. Refreshing...");
          setTimeout(() => {
            window.location.reload();
          }, 1500);
          return;
          
          // throw new Error("Asset has been deleted");
        }
      } catch (error) {

        toast.error("Asset has been deleted. Refreshing...");
          setTimeout(() => {
            window.location.reload();
          }, 1500);
          return;
        // window.location.reload();
        // throw new Error("Asset has been deleted");
      }


      toast.success("Starting download..."); // to download one asset from project page
      await downloadAsset(
        asset,
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
      // searchWithinOneProject: String(withinOneProjectSearchQuery), // sean
    });

    if (startDate) {
      queryParams.append("fromDate", getStartOfDayUtc(startDate));
    }

    if (endDate) {
      queryParams.append("toDate", getEndOfDayUtc(endDate));
    }

    if (withinOneProjectSearchQuery.trim() !== "") {
      queryParams.append("searchWithinOneProjectQuery", withinOneProjectSearchQuery.trim());
    }

    const url = `projects/${projectID}/assets/pagination?${queryParams.toString()}`;
    console.log("Query URL:", url); // log this right before the fetch
    const response = await fetchWithAuth(url);

    if (!response.ok) {
      console.error(
        `Failed to fetch assets (Status: ${response.status} - ${response.statusText})`
      );
      return { assets: [], totalPages: 0 };
    }

    const data = (await response.json()) as PaginatedAssets;
    console.log("Fetched assets:", data.assets);



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
  };

  const setAssetSrcs = ( // renders assets onto the screen
    assets: AssetWithSrc[],
    assetBlobSASUrlList: string[]
  ) => {
    assets.forEach(async (asset: AssetWithSrc, index: number) => {
      try {
        const src = (await getAssetFile(
          assetBlobSASUrlList[index],
          asset.mimetype || ""
        )) as string;
        setCurrentItems((prevItems: AssetWithSrc[]) =>
          prevItems.map((item: AssetWithSrc) =>
            item.blobID === asset.blobID ? { ...item, src } : item
          )
        );
      } catch (error) {
        console.error(`Error loading asset ${asset.blobID}:`, error);
      }
    });
  };

  const handlePageChange = (e: any, page: number) => {
    setCurrentPage(page);
    fetchAssets(page).then(({ assets, assetBlobSASUrlList, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets, assetBlobSASUrlList! as string[]);
    });
  };

  const handleDeleteAsset = (asset: AssetWithSrc) => {
    setAssetToDelete(asset);
    setConfirmDeletePopup(true);
  };

  const confirmDelete = async () => {
    if (!assetToDelete) return;
    // try {
    //   const checkResponse = await fetchWithAuth(`/projects/${projectID}/${assetToDelete.blobID}`, { // logic moved to backend (i.e. ProjectController.DeleteAsset will resolve with status code 410 GONE)
    //       method: "GET",
    //     });

    //     if (!checkResponse.ok) {
    //       toast.error("Asset has already been deleted. Refreshing...");
    //       setTimeout(() => {
    //         // window.location.reload();
    //       }, 1500);
    //       return;
    //     }  
    // } catch (error) {

    // }
    try {
      const response = await fetchWithAuth(
        `projects/${projectID}/assets/${assetToDelete.blobID}`,
        { method: "DELETE" }
      );
      if (response.status === 410) {
        toast.error("Asset has already been deleted. Refreshing...");
        setTimeout(() => {
          window.location.reload();
        }, 1500);
        return;
      }
      if (!response.ok) {
        throw new Error("Failed to delete asset");
      }
      setCurrentItems((prev) =>
        prev.filter(
          (item: AssetWithSrc) => item.blobID !== assetToDelete.blobID
        )
      );
      toast.success("Asset deleted successfully");
    } catch (error) {
      toast.error((error as Error).message);
    } finally {
      setConfirmDeletePopup(false);
      setAssetToDelete(null);
    }
  };

  useEffect(() => {
    // upon filter change we go back to page 1
    setCurrentPage(1);
    fetchAssets(1).then(({ assets, assetBlobSASUrlList, totalPages }) => {
      setCurrentItems(assets);
      setTotalPages(totalPages);
      setAssetSrcs(assets, assetBlobSASUrlList! as string[]);
    });
  }, [selectedUser, selectedTag, selectedAssetType, startDate, endDate, withinOneProjectSearchQuery]);

  

  useEffect(() => {
    getProject()
      .then((project: ProjectWithTags) => {
        console.log({project});
        setUsers(project.admins.concat(project.regularUsers));
        setProjectName(project.name!);
        setProjectDescription(project.description);
        const adminFound = project.admins.some(
          (admin: any) => admin.userID === user?.userID
        );
        setIsAdmin(adminFound);
        const isSuperAdmin = user?.superadmin || false;
        setIsAdmin(adminFound || isSuperAdmin);
        setProjectMetadataFields(project.metadataFields!);
      })
      .catch((error) => {
        console.error("Error fetching project:", error);
      });
    getTags().then((tags: any) => {
      setTags(tags);
    });
  }, []);

  useEffect(() => {
    if (withinOneProjectSearchQuery.trim() === "") {
      fetchAssets(1).then(({ assets }) => setCurrentAssets(assets));
    }
  }, [withinOneProjectSearchQuery]);


  // const handleWithinOneProjectSearch = async () => {
  //   setIsLoading(true);
  //   // fetchAssets(1);
  //   try {
  //     const { assets } = await fetchAssets(1); // start from first page
  //     console.log("Fetched assets_handleWithinOneProjectSearch:", assets);
  //     setCurrentAssets(assets);
  //     // setAssetSrcs(assets, assetBlobSASUrlList);
  //     setSearchDone(true);
  //   } catch (err) {
  //     console.error("Search failed", err);
  //   } finally {
  //     setIsLoading(false);
  //   }
  // };

  const handleWithinOneProjectSearch = async () => {
    setIsLoading(true);
    try {
      const { assets, assetBlobSASUrlList } = await fetchAssets(1);

      setCurrentAssets(assets);

      if (assetBlobSASUrlList) {
        setAssetSrcs(assets, assetBlobSASUrlList);
      } else {
        console.warn("assetBlobSASUrlList is undefined");
    }

      setSearchDone(true);
    } catch (err) {
      console.error("Search failed", err);
    } finally {
      setIsLoading(false);
    }
  };
  

  
  

  return (
    <>
      <h1 className="text-2xl font-bold mb-4">{"Project: " + projectName}</h1>
      <h6 className="mb-4">{"Description: " + projectDescription}</h6>
      <div className="w-full md:w-1/3 flex items-center">
        <input
          id="search"
          type="text"
          placeholder="Search assets..."
          className="w-full rounded-lg py-2 px-4 text-gray-700 bg-white shadow-sm border border-transparent focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition ease-in-out duration-150"
          value={withinOneProjectSearchQuery}
          onChange={(e) => setWithinOneProjectSearchQuery(e.target.value)}
          // onKeyDown={(e) => {
          //   if (e.key === "Enter") {
          //     handleWithinOneProjectSearch();
          //   }
          // }} 
            // && doWithinOneProjectSearch()}
        />
        {/* {withinOneProjectSearchQuery.trim() !== "" && (
          <button
            onClick={handleWithinOneProjectSearch}
            className="ml-2 px-4 py-2 bg-blue-500 text-white rounded-lg transition hover:bg-blue-600 flex items-center"
          >
            {isLoading ? <LoadingSpinner className="h-5 w-5" /> : "Search"}
          </button>
        )} */}
      </div>
      <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
        <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
          <label className="text-gray-700 text-sm font-medium">
            Filter by User
          </label>
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
          <label className="text-gray-700 text-sm font-medium">
            Filter by Tag
          </label>
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
          <label className="text-gray-700 text-sm font-medium">
            Filter by Asset Type
          </label>
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
          <label className="text-gray-700 text-sm font-medium">
            Start Date
          </label>
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
      <Items
        currentItems={currentItems}
        openPreview={openPreview}
        downloadAssetConfirm={downloadAssetConfirm}
        deleteAsset={handleDeleteAsset}
        showAssetMetadata={showAssetMetadata}
        isAdmin={isAdmin}
      />
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
            onClose={() => downloadAssetWrapper(false, requestedDownloadAsset)}
            onConfirm={() => downloadAssetWrapper(true, requestedDownloadAsset)}
            messages={[]}
            canCancel={false}
          />
        </div>
      )}

      {confirmDeletePopup && assetToDelete && (
        <PopupModal
          isOpen={true}
          onClose={() => {
            setConfirmDeletePopup(false);
            setAssetToDelete(null);
          }}
          onConfirm={confirmDelete}
          title="Delete Asset"
          messages={["Are you sure you want to delete this asset?"]}
        />
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

      {isPreviewAssetMetadata && (
        <GenericForm
          title={"Custom Metadata: " + assetMetadataName}
          isModal={true}
          fields={assetMetadataFields}
          onSubmit={() => {}}
          onCancel={() => setIsPreviewAssetMetadata(false)}
          isEdit={false}
          noRequired={true}
          submitButtonText=""
        />
      )}
    </>
  );
};

export default ProjectsTable;
