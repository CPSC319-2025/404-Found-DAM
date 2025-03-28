"use client";

import React from "react";

interface LoadingSpinnerProps {
  message?: string;
  size?: number;
}

export default function LoadingSpinner({
  message = "Loading ...",
  size = 12,
}: LoadingSpinnerProps) {
  return (
    <div className="flex flex-col">
      <div
        className={`border-4 border-blue-500 border-t-transparent rounded-full animate-spin`}
        style={{ width: `${size * 4}px`, height: `${size * 4}px` }}
      />
      <p className="mt-4 text-blue-500 text-lg text-center">{message}</p>
    </div>
  );
}
