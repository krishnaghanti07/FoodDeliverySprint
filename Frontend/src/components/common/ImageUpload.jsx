import React, { useState, useRef, useEffect } from 'react';
import { Upload, X, Image as ImageIcon } from 'lucide-react';
import { uploadProfileImage, uploadRestaurantLogo, uploadMenuItemImage, previewImage } from '../../utils/imageUpload';

/**
 * Reusable Image Upload Component with Cloudinary integration
 * @param {Object} props
 * @param {string} props.type - 'profile', 'restaurant', or 'menuItem'
 * @param {string} props.currentImageUrl - Current image URL (for editing)
 * @param {Function} props.onImageUploaded - Callback when image is uploaded (receives Cloudinary URL)
 * @param {string} props.label - Label for the upload field
 * @param {string} props.className - Additional CSS classes
 */
const ImageUpload = ({ 
  type = 'menuItem', 
  currentImageUrl = '', 
  onImageUploaded, 
  label = 'Upload Image',
  className = '' 
}) => {
  const [preview, setPreview] = useState(currentImageUrl || '');
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const fileInputRef = useRef(null);

  // Update preview when currentImageUrl changes
  useEffect(() => {
    if (currentImageUrl) {
      setPreview(currentImageUrl);
    }
  }, [currentImageUrl]);

  const handleFileSelect = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError('');

    try {
      // Show local preview immediately
      const previewUrl = await previewImage(file);
      setPreview(previewUrl);

      // Upload to Cloudinary
      setUploading(true);
      let cloudinaryUrl;

      if (type === 'profile') {
        cloudinaryUrl = await uploadProfileImage(file);
      } else if (type === 'restaurant') {
        cloudinaryUrl = await uploadRestaurantLogo(file);
      } else {
        cloudinaryUrl = await uploadMenuItemImage(file);
      }

      // Update parent component with Cloudinary URL
      if (onImageUploaded) {
        onImageUploaded(cloudinaryUrl);
      }

      // Replace preview with Cloudinary URL
      setPreview(cloudinaryUrl);
      setUploading(false);
    } catch (err) {
      setError(err.message || 'Failed to upload image');
      setUploading(false);
      setPreview(currentImageUrl); // Revert to original
      
      // Clear the file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleRemove = () => {
    setPreview('');
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    if (onImageUploaded) {
      onImageUploaded('');
    }
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  return (
    <div className={`space-y-2 ${className}`}>
      <label className="block text-sm font-medium text-gray-700">
        {label}
      </label>

      <div className="flex items-start gap-4">
        {/* Preview Area */}
        <div className="relative">
          {preview ? (
            <div className={`relative ${type === 'profile' ? 'w-24 h-24 rounded-full' : 'w-32 h-32 rounded-lg'} overflow-hidden border-2 border-gray-200`}>
              <img
                src={preview}
                alt="Preview"
                className="w-full h-full object-cover"
              />
              {!uploading && (
                <button
                  type="button"
                  onClick={handleRemove}
                  className="absolute top-1 right-1 p-1 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors"
                  title="Remove image"
                >
                  <X size={16} />
                </button>
              )}
              {uploading && (
                <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white"></div>
                </div>
              )}
            </div>
          ) : (
            <div
              onClick={handleClick}
              className={`${type === 'profile' ? 'w-24 h-24 rounded-full' : 'w-32 h-32 rounded-lg'} border-2 border-dashed border-gray-300 flex flex-col items-center justify-center cursor-pointer hover:border-orange-500 hover:bg-orange-50 transition-colors`}
            >
              <ImageIcon size={32} className="text-gray-400 mb-2" />
              <span className="text-xs text-gray-500">Click to upload</span>
            </div>
          )}
        </div>

        {/* Upload Button and Info */}
        <div className="flex-1">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/jpg,image/png,image/webp,image/gif"
            onChange={handleFileSelect}
            className="hidden"
          />

          <button
            type="button"
            onClick={handleClick}
            disabled={uploading}
            className="flex items-center gap-2 px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
          >
            <Upload size={18} />
            {uploading ? 'Uploading...' : 'Choose Image'}
          </button>

          <p className="text-xs text-gray-500 mt-2">
            JPG, PNG, WEBP, or GIF (max 5MB)
          </p>

          {error && (
            <p className="text-xs text-red-500 mt-2">{error}</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default ImageUpload;
