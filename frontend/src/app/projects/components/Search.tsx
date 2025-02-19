import Link from "next/link";

export default function Search({}) {
  return (
    <input
      type="text"
      placeholder="Search..."
      className="w-1/3 border border-gray-300 rounded-lg py-2 px-4 focus:outline-none focus:border-blue-500"
    />
  );
}
