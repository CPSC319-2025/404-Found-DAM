"use client";

import React, { useState, useEffect, useCallback } from "react";
import Image from "next/image";
import { PencilIcon, TrashIcon } from "@heroicons/react/24/outline";
import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/react/24/solid";

const ProjectsTable = () => {
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;
  const totalItems = 7;
  const totalPages = Math.ceil(totalItems / itemsPerPage);

  const handleRowClick = (imageId: string) => {
    console.log(`Clicked row with image ID: ${imageId}`);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const [selectedStatus, setSelectedStatus] = useState("");
  const [selectedPostedBy, setSelectedPostedBy] = useState("");
  const [selectedDate, setSelectedDate] = useState("");
  const [searchTags, setSearchTags] = useState("");

  const initialImages = [
    {
      id: "1",
      status: "inactive",
      postedBy: "John Doe",
      datePosted: "2025-01-26",
      tags: ["construction", "building"],
      thumbnail: "/images/image1.jpg",
    },
    {
      id: "2",
      status: "inactive",
      postedBy: "Jane Smith",
      datePosted: "2025-01-29",
      tags: ["office", "building"],
      thumbnail: "/images/image2.jpg",
    },
    {
      id: "3",
      status: "active",
      postedBy: "Hoi",
      datePosted: "2025-02-26",
      tags: ["sports", "soccer", "ball"],
      thumbnail: "/images/image3.jpg",
    },
    {
      id: "4",
      status: "active",
      postedBy: "Hoi",
      datePosted: "2025-02-26",
      tags: [],
      thumbnail: "/images/image4.jpg",
    },
    {
      id: "5",
      status: "active",
      postedBy: "Hoi",
      datePosted: "2025-02-26",
      tags: ["travel", "fast", "usa"],
      thumbnail: "/images/image5.jpg",
    },
    {
      id: "6",
      status: "active",
      postedBy: "Hoi",
      datePosted: "2025-02-26",
      tags: [],
      thumbnail: "/images/image6.jpg",
    },
    {
      id: "7",
      status: "active",
      postedBy: "Hoi",
      datePosted: "2025-02-26",
      tags: [],
      thumbnail: "/images/image7.jpg",
    },
  ];

  const [originalImages, setOriginalImages] = useState(initialImages);
  const [images, setImages] = useState(initialImages);

  const applyFilters = useCallback(() => {
    let filtered = [...originalImages];

    // Status filter
    if (selectedStatus && selectedStatus !== "All") {
      filtered = filtered.filter((image) => image.status === selectedStatus);
    }

    // Posted By filter
    if (selectedPostedBy) {
      filtered = filtered.filter(
        (image) => image.postedBy === selectedPostedBy
      );
    }

    // Date filter
    if (selectedDate) {
      filtered = filtered.filter((image) => image.datePosted === selectedDate);
    }

    // Tags filter with multiple tag support
    if (searchTags) {
      const tagsArray = searchTags
        .split(",")
        .map((tag) => tag.trim().toLowerCase())
        .filter((tag) => tag.length > 0);
      if (tagsArray.length > 0) {
        filtered = filtered.filter((image) =>
          image.tags.some((tag) =>
            tagsArray.some((searchTag) => tag.toLowerCase().includes(searchTag))
          )
        );
      }
    }

    setImages(filtered);
    setCurrentPage(1); // Reset to first page on filter change
  }, [
    originalImages,
    selectedStatus,
    selectedPostedBy,
    selectedDate,
    searchTags,
  ]);

  useEffect(() => {
    applyFilters();
  }, [applyFilters]);

  return (
    <div>
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between mb-6 bg-white w-full h-full p-3">
        <div className="flex flex-col md:flex-row items-start md:items-center gap-4 w-full">
          <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
            <select
              className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={selectedStatus}
              onChange={(e) => {
                setSelectedStatus(e.target.value);
                if (e.target.value === "") {
                  setImages(originalImages);
                } else {
                  const filteredImages = originalImages.filter(
                    (image) => image.status === e.target.value
                  );
                  setImages(filteredImages);
                }
                setCurrentPage(1);
              }}
            >
              <option value="">Filter by Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>
          <div className="w-full md:flex-1 min-w-0 md:min-w-[150px] mb-4 md:mb-0">
            <select
              className="w-full px-3 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={selectedPostedBy}
              onChange={(e) => setSelectedPostedBy(e.target.value)}
            >
              <option value="">Filter by Posted By</option>
              <option value="John Doe">John Doe</option>
              <option value="Jane Smith">Jane Smith</option>
              <option value="Hoi">Hoi</option>
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
              value={searchTags}
              onChange={(e) => setSearchTags(e.target.value)}
            />
          </div>
        </div>
      </div>
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
                Active/Inactive
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Modify
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {images.map((image) => (
              <tr
                key={image.id}
                onClick={() => handleRowClick(image.id)}
                className="cursor-pointer hover:bg-gray-50"
              >
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {image.id}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="h-20 w-20 relative">
                    <Image
                      src={image.thumbnail}
                      alt={`${image.name} thumbnail`}
                      width={120}
                      height={120}
                      className="object-cover rounded w-full h-full"
                    />
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {image.datePosted}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">{image.postedBy}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex gap-1">
                    {image.tags.map((tag) => (
                      <span
                        key={tag}
                        className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      image.status === "active"
                        ? "bg-green-100 text-green-800"
                        : "bg-red-100 text-red-800"
                    }`}
                  >
                    {image.status.charAt(0).toUpperCase() +
                      image.status.slice(1)}
                  </span>
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
                          const updatedImages = images.filter(
                            (img) => img.id !== image.id
                          );
                          setImages(updatedImages);
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

      {/* Pagination */}
      <div className="flex items-center justify-between border-t border-gray-200 bg-white px-4 py-3 sm:px-6">
        <div className="flex flex-1 justify-between sm:hidden">
          <button
            onClick={() => handlePageChange(currentPage - 1)}
            disabled={currentPage === 1}
            className="relative inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Previous
          </button>
          <button
            onClick={() => handlePageChange(currentPage + 1)}
            disabled={currentPage === totalPages}
            className="relative ml-3 inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Next
          </button>
        </div>
        <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
          <div>
            <p className="text-sm text-gray-700">
              Showing{" "}
              <span className="font-medium">
                {(currentPage - 1) * itemsPerPage + 1}
              </span>{" "}
              to{" "}
              <span className="font-medium">
                {Math.min(currentPage * itemsPerPage, totalItems)}
              </span>{" "}
              of <span className="font-medium">{totalItems}</span> results
            </p>
          </div>
          <div>
            <nav
              className="isolate inline-flex -space-x-px rounded-md shadow-sm"
              aria-label="Pagination"
            >
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={currentPage === 1}
                className="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span className="sr-only">Previous</span>
                <ChevronLeftIcon className="h-5 w-5" aria-hidden="true" />
              </button>
              {[...Array(totalPages)].map((_, index) => (
                <button
                  key={index + 1}
                  onClick={() => handlePageChange(index + 1)}
                  className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                    currentPage === index + 1
                      ? "z-10 bg-indigo-600 text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
                      : "text-gray-900 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0"
                  }`}
                >
                  {index + 1}
                </button>
              ))}
              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={currentPage === totalPages}
                className="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span className="sr-only">Next</span>
                <ChevronRightIcon className="h-5 w-5" aria-hidden="true" />
              </button>
            </nav>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProjectsTable;
