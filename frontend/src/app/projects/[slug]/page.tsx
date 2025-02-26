import ProjectsTable from "../components/ProjectsTable";

type ProjectPageProps = {
  params: { slug: string };
};

export default function ProjectPage({ params }: ProjectPageProps) {
  return (
    <div className="p-6 min-h-screen">
      <ProjectsTable />
    </div>
  );
}
