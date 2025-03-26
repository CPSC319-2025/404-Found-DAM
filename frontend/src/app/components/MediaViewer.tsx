"use client";

import React, { useState, useEffect, useRef } from 'react';

interface MediaViewerProps {
  blobId: string;
  mimeType: string;
  fileName: string;
  className?: string;
  previewMode?: boolean;
}

export const MediaViewer: React.FC<MediaViewerProps> = ({
  blobId,
  mimeType,
  fileName,
  className = '',
  previewMode = false,
}) => {
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const mediaRef = useRef<HTMLVideoElement | null>(null);
  
  const isImage = mimeType.startsWith('image/');
  const isVideo = mimeType.startsWith('video/');
  
  // Generate the appropriate source URL
  const baseUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL}/palette/assets/${blobId}`;
  // Both images and videos should use decompress=true for proper display
  const sourceUrl = `${baseUrl}?decompress=true`;
  
  // Handle loading errors
  const handleError = (e: React.SyntheticEvent<HTMLVideoElement | HTMLImageElement>) => {
    console.error(`Error loading ${isImage ? 'image' : 'video'} for blobId: ${blobId}`, e);
    console.error(`Source URL: ${sourceUrl}`);
    setError(`Failed to load ${isImage ? 'image' : 'video'}`);
    setIsLoading(false);
  };
  
  const handleLoad = () => {
    console.log(`Successfully loaded ${isImage ? 'image' : 'video'} for blobId: ${blobId}`);
    setIsLoading(false);
  };
  
  // For video optimization
  useEffect(() => {
    if (isVideo && mediaRef.current) {
      // Only for videos - configure the player to use range requests optimally
      const videoElement = mediaRef.current;
      
      // Set lower initial quality to start playback faster
      videoElement.addEventListener('canplay', () => {
        // Adjust video quality based on network conditions if the API is available
        if ('connection' in navigator) {
          const connection = (navigator as any).connection;
          if (connection && connection.effectiveType === '4g') {
            // On good connections, increase quality after initial buffering
            setTimeout(() => {
              if (videoElement.readyState >= 3) {
                videoElement.playbackRate = 1.0;
              }
            }, 3000);
          }
        }
      });
      
      // Preload only metadata initially to reduce initial load
      videoElement.preload = previewMode ? 'none' : 'metadata';
      
      // For preview mode, don't auto-download the video
      if (previewMode) {
        videoElement.controls = false;
        videoElement.autoplay = false;
      }
    }
  }, [isVideo, previewMode]);
  
  if (error) {
    return <div className="text-red-500">{error}</div>;
  }
  
  return (
    <div className={`relative ${className}`}>
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-100 bg-opacity-50">
          <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
        </div>
      )}
      
      {isImage && (
        <img
          src={sourceUrl}
          alt={fileName}
          className={`${className} ${previewMode ? 'object-cover' : 'max-w-full h-auto'}`}
          loading={previewMode ? 'lazy' : 'eager'}
          onLoad={handleLoad}
          onError={handleError}
        />
      )}
      
      {isVideo && (
        <video
          ref={mediaRef}
          className={`${className} ${previewMode ? 'object-cover' : 'max-w-full h-auto'}`}
          controls={!previewMode}
          playsInline
          onLoadedMetadata={handleLoad}
          onError={handleError}
          preload={previewMode ? 'none' : 'metadata'}
        >
          {/* Provide multiple source formats for better compatibility */}
          <source src={sourceUrl} type={mimeType} />
          {/* Fallback for different browsers */}
          {mimeType === 'video/mp4' && (
            <source src={`${baseUrl}?decompress=true&altFormat=webm`} type="video/webm" />
          )}
          {mimeType === 'video/webm' && (
            <source src={`${baseUrl}?decompress=true&altFormat=mp4`} type="video/mp4" />
          )}
          Your browser does not support the video tag.
        </video>
      )}
    </div>
  );
};

export default MediaViewer; 