import ProjectCard from "./components/ProjectCard";

const projects = [
  { id: "1", name: "Project One" },
  { id: "2", name: "Project Two" },
  { id: "3", name: "Project Three" },
  { id: "4", name: "Project Four" },
  { id: "5", name: "Project Five" },
];

export default function ProjectsPage() {
  return (
    <div>
      <h1 className="text-2xl font-bold mb-4">Projects</h1>
      <div className="grid grid-cols-1 sm:grid-cols-[repeat(auto-fill,_minmax(320px,_1fr))] lg:grid-cols-[repeat(auto-fill,_minmax(320px,_420px))] gap-4">
        {projects.map((project) => (
          <div key={project.id} className="w-full h-full">
            <ProjectCard id={project.id} name={project.name} />
          </div>
        ))}
      </div>
    </div>
  );
}
