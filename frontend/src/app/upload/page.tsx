import Link from "next/link";


export default function UploadPage() {
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
                            </td>
                        </tr>
                        <tr className="border-b hover:bg-gray-50">
                            <td className="py-2">
                                <input type="checkbox" />
                            </td>
                            <td className="py-2">Big Dom</td>
                            <td className="py-2">JPG</td>
                            <td className="py-2">10 MB</td>
                            <td className="py-2 flex space-x-2">
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
                            </td>
                        </tr>
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
