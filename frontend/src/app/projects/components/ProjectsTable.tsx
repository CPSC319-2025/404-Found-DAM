'use client';

import React from 'react';
import Image from 'next/image';
import { PencilIcon, TrashIcon } from '@heroicons/react/24/outline';

const ProjectsTable = () => {
	const handleRowClick = (imageId: string) => {
		console.log(`Clicked row with image ID: ${imageId}`);
	};

	return (
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
					<tr
						onClick={() => handleRowClick('IMG_001')}
						className="cursor-pointer hover:bg-gray-50"
					>
						<td className="px-6 py-4 whitespace-nowrap">
							<div className="text-sm font-medium text-gray-900">IMG_001</div>
						</td>
						<td className="px-6 py-4 whitespace-nowrap">
							<div className="h-20 w-20 relative">
								<Image
									src="/images/project1image1.jpeg"
									alt="Asset thumbnail"
									width={120}
									height={120}
									className="object-cover rounded w-full h-full"
								/>
							</div>
						</td>
						<td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
							2024-03-20
						</td>
						<td className="px-6 py-4 whitespace-nowrap">
							<div className="text-sm text-gray-900">John Doe</div>
						</td>
						<td className="px-6 py-4 whitespace-nowrap">
							<div className="flex gap-1">
								<span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
									landscape
								</span>
								<span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-purple-100 text-purple-800">
									nature
								</span>
							</div>
						</td>
						<td className="px-6 py-4 whitespace-nowrap">
							<span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">
								Active
							</span>
						</td>
						<td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
							<div className="flex gap-3">
								<button
									className="text-indigo-600 hover:text-indigo-900"
									onClick={e => {
										e.stopPropagation();
										// EDIT LOGIC
									}}
								>
									<PencilIcon className="h-5 w-5" />
								</button>
								<button
									className="text-red-600 hover:text-red-900"
									onClick={e => {
										e.stopPropagation();
										// DELETE LOGIC
									}}
								>
									<TrashIcon className="h-5 w-5" />
								</button>
							</div>
						</td>
					</tr>
				</tbody>
			</table>
		</div>
	);
};

export default ProjectsTable;
