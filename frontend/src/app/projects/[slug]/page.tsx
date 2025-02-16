import Image from 'next/image';
import ProjectsTable from '../components/ProjectsTable';

// This function tells Next.js which dynamic routes to generate
export async function generateStaticParams() {
	// Replace this hardcoded array with your actual data fetching logic if needed.
	return [{ slug: 'project-1' }, { slug: 'project-2' }];
}
type ProjectPageProps = {
	params: { slug: string };
};

export default function ProjectPage({ params }: ProjectPageProps) {
	return <ProjectsTable />;
}
