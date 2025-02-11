import Link from "next/link";

interface ProjectCardProps {
  name: string;
  id: string;
}

export default function ProjectCard({name, id}: ProjectCardProps) {
  return (
    <Link href={`/projects/${id}`} passHref>
      <div className="border p-4 rounded-lg shadow-md hover:shadow-lg transition-shadow duration-300 cursor-pointer">
        <h3 className="text-xl font-semibold">{name}</h3>
        <div className="mt-4">
          <span className="text-blue-500 hover:text-blue-700 font-medium">
            View Project Details
          </span>
        </div>
      </div>
    </Link>
  );
}
