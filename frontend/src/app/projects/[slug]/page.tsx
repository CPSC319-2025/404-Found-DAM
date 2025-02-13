// src/app/projects/[slug]/page.tsx

// This function tells Next.js which dynamic routes to generate
export async function generateStaticParams() {
  // Replace this hardcoded array with your actual data fetching logic if needed.
  return [{ slug: 'project-1' }, { slug: 'project-2' }];
}

type ProjectPageProps = {
  params: { slug: string };
};

export default function ProjectPage({ params }: ProjectPageProps) {
  return (
    <div>
      <h1 className="text-2xl font-bold">Project {params.slug}</h1>
      <p>Details about project {params.slug}...</p>
    </div>
  );
}
