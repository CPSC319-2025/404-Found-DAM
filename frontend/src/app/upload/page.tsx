"use client"; 
import Link from "next/link";
import { useState } from "react";

export default function UploadPage() {

    const [tags, setTags] = useState({
        "Company logo": ["logo", "bg", "Beth Jacobs","Project 3", "Project 2"],
        "Big Dom": ["logo", "bg", "Beth Jacobs", "Project 3", "Project 2"],
      });

      const removeTag = (fileName: string | number, tagToRemove: any) => {
        setTags((prevTags) => {
          const updatedTags = { ...prevTags };
          updatedTags[fileName]  = updatedTags[fileName].filter((tag: any) => tag !== tagToRemove);
          return updatedTags;
        });
      };


      const [status, setStatus] = useState({
        "Project 3": { tick: false, cross: false }, // Initial state for tick and cross
      });
    
        // Handle the toggle of tick/cross circles
  const handleCircleClick = (project: string, type: "tick" | "cross") => {
    setStatus((prevStatus) => ({
      ...prevStatus,
      [project]: {
        ...prevStatus[project],
        [type]: !prevStatus[project][type], // Toggle the tick or cross
      },
    }));
  };
    
    return (
        <div className="flex flex-col min-h-screen bg-gray-100 text-gray-800">
            {/* Header / Top Bar */}
            <header className="flex items-center justify-end bg-white p-4 shadow-md">
                <div className="text-sm">
                    <span className="font-medium">John Smith</span>
                </div>
            </header>

            {/* Main Content */}
            <main className="flex-grow p-6">
                {/* Table of Files */}
                <section className="bg-white p-4 rounded shadow-md mb-6">
                    <table className="w-full text-left">
                        <thead>
                        <tr className="border-b">
                            <th className="py-2">
                                <input type="checkbox" />
                            </th>
                            <th className="py-2">File Name</th>
                            <th className="py-2">File Type</th>
                            <th className="py-2">File Size</th>
                            <th className="py-2">Tags</th>
                            <th className="py-2">Projects</th>
                        </tr>
                        </thead>
                        <tbody>
                        <tr className="border-b hover:bg-gray-50">
                            <td className="py-2">
                                <input type="checkbox" />
                            </td>
                            <td className="py-2">Company logo</td>
                            <td className="py-2">PNG</td>
                            <td className="py-2">10 MB</td>
                            <td className="py-2 flex space-x-2">
                            {tags["Company logo"]?.map((tag) => (
                    <span
                      key={tag}
                      className="bg-gray-200 px-2 py-1 rounded text-sm flex items-center space-x-2"
                    >
                      <span>{tag}</span>
                      <button
                        onClick={() => removeTag("Company logo", tag)}
                        className="text-xs text-gray-500 ml-2"
                      >
                        x
                      </button>
                    </span>
                  ))}
                </td>
                <td className="py-2">
                  {/* Flex container to align buttons on the left and the label with circles on the right */}
                  <div className="flex justify-between items-center">
                    <div className="flex flex-col space-y-2">
                      <button className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600">
                        Edit Assets
                      </button>
                      <button className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600">
                        Edit Metadata
                      </button>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="text-sm font-medium">Project 3</span>
                      {/* Circle with Tick (clickable) */}
                      <div
                        onClick={() => handleCircleClick("Project 3", "tick")}
                        className={`w-6 h-6 flex items-center justify-center rounded-full cursor-pointer ${
                          status["Project 3"]?.tick ? "bg-green-500" : "bg-gray-300"
                        }`}
                      >
                        <span className="text-xl">{status["Project 3"]?.tick ? "✔" : ""}</span>
                      </div>
                      {/* Circle with Cross (clickable) */}
                      <div
                        onClick={() => handleCircleClick("Project 3", "cross")}
                        className={`w-6 h-6 flex items-center justify-center rounded-full cursor-pointer ${
                          status["Project 3"]?.cross ? "bg-red-500" : "bg-gray-300"
                        }`}
                      >
                        <span className="text-xl">{status["Project 3"]?.cross ? "✖" : ""}</span>
                      </div>
                    </div>
                  </div>
                </td>

{/*                             <td className="py-2 flex space-x-2">
                  <span className="bg-gray-200 px-2 py-1 rounded text-sm">
                    logo
                  </span>
                                <span className="bg-gray-200 px-2 py-1 rounded text-sm">
                    bg
                  </span>
                            </td>
                             <td className="py-2 flex space-x-2">
                  <span className="bg-black text-white px-2 py-1 rounded text-sm">
                    Beth Jacobs
                  </span>
                                <span className="bg-gray-300 px-2 py-1 rounded text-sm">
                    Project 3
                  </span>
                                <span className="bg-gray-300 px-2 py-1 rounded text-sm">
                    Project 2
                  </span>
                            </td>*/}
                        </tr>
                        <tr className="border-b hover:bg-gray-50">
                            <td className="py-2">
                                <input type="checkbox" />
                            </td>
                            <td className="py-2">Big Dom</td>
                            <td className="py-2">JPG</td>
                            <td className="py-2">10 MB</td>
                            <td className="py-2 flex space-x-2">
                            {tags["Big Dom"]?.map((tag) => (
                    <span
                      key={tag}
                      className="bg-gray-200 px-2 py-1 rounded text-sm flex items-center space-x-2"
                    >
                      <span>{tag}</span>
                      <button
                        onClick={() => removeTag("Big Dom", tag)}
                        className="text-xs text-gray-500 ml-2"
                      >
                        x
                      </button>
                    </span>
                  ))}
                </td>

                <td className="py-2">
                  {/* Flex container to align buttons on the left and the label with circles on the right */}
                  <div className="flex justify-between items-center">
                    <div className="flex flex-col space-y-2">
                      <button className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600">
                        Edit Assets
                      </button>
                      <button className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600">
                        Edit Metadata
                      </button>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="text-sm font-medium">Project 3</span>
                      {/* Circle with Tick (clickable) */}
                      <div
                        onClick={() => handleCircleClick("Project 3", "tick")}
                        className={`w-6 h-6 flex items-center justify-center rounded-full cursor-pointer ${
                          status["Project 3"]?.tick ? "bg-green-500" : "bg-gray-300"
                        }`}
                      >
                        <span className="text-xl">{status["Project 3"]?.tick ? "✔" : ""}</span>
                      </div>
                      {/* Circle with Cross (clickable) */}
                      <div
                        onClick={() => handleCircleClick("Project 3", "cross")}
                        className={`w-6 h-6 flex items-center justify-center rounded-full cursor-pointer ${
                          status["Project 3"]?.cross ? "bg-red-500" : "bg-gray-300"
                        }`}
                      >
                        <span className="text-xl">{status["Project 3"]?.cross ? "✖" : ""}</span>
                      </div>
                    
                    </div>
                  </div>
                </td>


{/*                             <td className="py-2 flex space-x-2">
                  <span className="bg-gray-200 px-2 py-1 rounded text-sm">
                    logo
                  </span>
                                <span className="bg-gray-200 px-2 py-1 rounded text-sm">
                    bg
                  </span>
                            </td>
 */}
                            
{/*                             <td className="py-2 flex space-x-2">
                  <span className="bg-black text-white px-2 py-1 rounded text-sm">
                    Beth Jacobs
                  </span>
                                <span className="bg-gray-300 px-2 py-1 rounded text-sm">
                    Project 3
                  </span>
                                <span className="bg-gray-300 px-2 py-1 rounded text-sm">
                    Project 2
                  </span>
                            </td>
 */}                        </tr>
                        </tbody>
                    </table>
                </section>

                {/* Drag & Drop Section */}
                <section className="bg-white p-8 rounded shadow-md text-center">
                    <div className="border-2 border-dashed border-gray-300 p-8 rounded-lg mb-4">
                        <p className="text-xl mb-2">Drag and Drop here</p>
                        <p className="text-gray-500 mb-4">or</p>
                        <button className="bg-teal-500 text-white px-4 py-2 rounded hover:bg-teal-600">
                            Select files!
                        </button>
                    </div>
                    <button className="bg-black text-white px-4 py-2 rounded hover:bg-gray-800">
                        Upload Assets
                    </button>
                </section>
            </main>
        </div>
    );
}
