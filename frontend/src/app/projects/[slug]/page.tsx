import ProjectsTable from '../components/ProjectsTable';

type ProjectPageProps = {
	params: { slug: string };
};

export default function ProjectPage({ params }: ProjectPageProps) {
	return <ProjectsTable />;
}
