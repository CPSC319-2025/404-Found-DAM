import Link from "next/link";

interface ProjectCardProps {
  name: string;
  id: string;
}

const isAdmin = true;

export default function ProjectCard({ name, id }: ProjectCardProps) {
	return (
		<Link href={`/projects/${id}`} passHref>
			<div className="border p-4 rounded-lg hover:shadow-lg transition-shadow duration-300 cursor-pointer bg-white shadow-sm">
				<div className="flex flex-col gap-4 p-4">
					<div className="flex items-center justify-between">
						<div className="flex items-center gap-3">
							<div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
								<svg
									xmlns="http://www.w3.org/2000/svg"
									viewBox="0 0 24 24"
									fill="currentColor"
									className="size-6"
								>
									<path d="M19.5 21a3 3 0 0 0 3-3v-4.5a3 3 0 0 0-3-3h-15a3 3 0 0 0-3 3V18a3 3 0 0 0 3 3h15ZM1.5 10.146V6a3 3 0 0 1 3-3h5.379a2.25 2.25 0 0 1 1.59.659l2.122 2.121c.14.141.331.22.53.22H19.5a3 3 0 0 1 3 3v1.146A4.483 4.483 0 0 0 19.5 9h-15a4.483 4.483 0 0 0-3 1.146Z" />
								</svg>
							</div>
							<div>
								<p className="text-l font-semibold">{name}</p>
								<p className="text-sm text-gray-500">
									{new Date().toLocaleDateString()}
									{', '}
									{new Date().toLocaleTimeString()}
								</p>
							</div>
						</div>
						<Link 
							href={`/projects/edit/${id}`} 
							onClick={(e) => e.stopPropagation() }
							className="flex items-center justify-center w-8 h-8 hover:bg-gray-100 rounded-full cursor-pointer"
						>
							<svg
								xmlns="http://www.w3.org/2000/svg"
								viewBox="0 0 24 24"
								fill="currentColor"
								className="size-6"
							>
								<path
									fillRule="evenodd"
									d="M10.5 6a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0Zm0 6a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0Zm0 6a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0Z"
									clipRule="evenodd"
								/>
							</svg>
						</Link>
					</div>
					<div>
						<div className="flex justify-between items-center">
							<p className="text-sm text-gray-500">Shared Users</p>
							<p className="text-sm text-gray-500">Inside Files</p>
						</div>
						<div className="flex items-center justify-between mt-2">
							<div className="relative flex -space-x-2">
								{[...Array(4)].map((_, index) => (
									<div
										key={index}
										className="w-8 h-8 rounded-full bg-gray-200 border-2 border-white flex items-center justify-center"
									>
										<svg
											xmlns="http://www.w3.org/2000/svg"
											viewBox="0 0 24 24"
											fill="currentColor"
											className="w-5 h-5 text-gray-600"
										>
											<path
												fillRule="evenodd"
												d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z"
												clipRule="evenodd"
											/>
										</svg>
									</div>
								))}
								<div className="w-8 h-8 rounded-full bg-blue-100 border-2 border-white flex items-center justify-center text-sm font-medium text-blue-600">
									+52
								</div>
							</div>
							<div className="w-16 h-8 rounded-md bg-blue-100 border-2 border-white flex items-center justify-center text-sm font-medium text-blue-600">
								128
							</div>
						</div>
					</div>
				</div>
			</div>
		</Link>
	);
}
