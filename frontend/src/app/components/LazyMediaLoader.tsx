"use client";

import React, { useState, useEffect, useRef } from 'react';
import MediaViewer from './MediaViewer';

interface LazyMediaLoaderProps {
  blobId: string;
  mimeType: string;
  fileName: string;
  className?: string;
  placeholderClassName?: string;
  threshold?: number;
}

const LazyMediaLoader: React.FC<LazyMediaLoaderProps> = ({
  blobId,
  mimeType,
  fileName,
  className = '',
  placeholderClassName = 'h-40 w-full bg-gray-200 animate-pulse rounded',
  threshold = 0.1,
}) => {
  const [isVisible, setIsVisible] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Skip if SSR (no window) or no IntersectionObserver support
    if (typeof window === 'undefined' || !('IntersectionObserver' in window)) {
      setIsVisible(true);
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          setIsVisible(true);
          if (containerRef.current) {
            observer.unobserve(containerRef.current);
          }
        }
      },
      {
        threshold, // Load when at least 10% of the element is visible
        rootMargin: '100px', // Start loading when element is 100px from viewport
      }
    );

    if (containerRef.current) {
      observer.observe(containerRef.current);
    }

    return () => {
      if (containerRef.current) {
        observer.unobserve(containerRef.current);
      }
    };
  }, [threshold]);

  return (
    <div ref={containerRef} className={className}>
      {isVisible ? (
        <MediaViewer
          blobId={blobId}
          mimeType={mimeType}
          fileName={fileName}
          className={className}
          previewMode={true}
        />
      ) : (
        <div className={placeholderClassName} aria-label={`Loading ${fileName}`} />
      )}
    </div>
  );
};

export default LazyMediaLoader; 