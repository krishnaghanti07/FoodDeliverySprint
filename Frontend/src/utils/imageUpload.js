import api from '../services/api';

/**
 * Upload profile image to Cloudinary via backend (for all user roles)
 * @param {File} file - The image file to upload
 * @returns {Promise<string>} - The Cloudinary URL of the uploaded image
 */
export const uploadProfileImage = async (file) => {
  try {
    // Validate file
    if (!file) {
      throw new Error('No file provided');
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      throw new Error('Invalid file type. Only JPG, PNG, WEBP, and GIF are allowed.');
    }

    // Validate file size (5MB max)
    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      throw new Error('File size exceeds 5MB limit.');
    }

    // Create FormData
    const formData = new FormData();
    formData.append('file', file);

    // Upload to backend
    const response = await api.post('/gateway/auth/profile-image/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    if (response.data.success && response.data.data) {
      return response.data.data; // Return the Cloudinary URL
    }

    throw new Error(response.data.message || 'Failed to upload image');
  } catch (error) {
    console.error('Error uploading profile image:', error);
    throw error;
  }
};

/**
 * Upload restaurant logo to Cloudinary via backend
 * @param {File} file - The image file to upload
 * @returns {Promise<string>} - The Cloudinary URL of the uploaded image
 */
export const uploadRestaurantLogo = async (file) => {
  try {
    // Validate file
    if (!file) {
      throw new Error('No file provided');
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      throw new Error('Invalid file type. Only JPG, PNG, WEBP, and GIF are allowed.');
    }

    // Validate file size (5MB max)
    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      throw new Error('File size exceeds 5MB limit.');
    }

    // Create FormData
    const formData = new FormData();
    formData.append('file', file);

    // Upload to backend
    const response = await api.post('/gateway/catalog/images/restaurant-logo', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    if (response.data.success && response.data.data) {
      return response.data.data; // Return the Cloudinary URL
    }

    throw new Error(response.data.message || 'Failed to upload image');
  } catch (error) {
    console.error('Error uploading restaurant logo:', error);
    throw error;
  }
};

/**
 * Upload menu item image to Cloudinary via backend
 * @param {File} file - The image file to upload
 * @returns {Promise<string>} - The Cloudinary URL of the uploaded image
 */
export const uploadMenuItemImage = async (file) => {
  try {
    // Validate file
    if (!file) {
      throw new Error('No file provided');
    }

    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      throw new Error('Invalid file type. Only JPG, PNG, WEBP, and GIF are allowed.');
    }

    // Validate file size (5MB max)
    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      throw new Error('File size exceeds 5MB limit.');
    }

    // Create FormData
    const formData = new FormData();
    formData.append('file', file);

    // Upload to backend
    const response = await api.post('/gateway/catalog/images/menu-item', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    if (response.data.success && response.data.data) {
      return response.data.data; // Return the Cloudinary URL
    }

    throw new Error(response.data.message || 'Failed to upload image');
  } catch (error) {
    console.error('Error uploading menu item image:', error);
    throw error;
  }
};

/**
 * Delete an image from Cloudinary via backend
 * @param {string} imageUrl - The Cloudinary URL to delete
 * @returns {Promise<boolean>} - True if deletion was successful
 */
export const deleteImage = async (imageUrl) => {
  try {
    if (!imageUrl) {
      return false;
    }

    const response = await api.delete('/gateway/catalog/images', {
      params: { imageUrl },
    });

    return response.data.success;
  } catch (error) {
    console.error('Error deleting image:', error);
    return false;
  }
};

/**
 * Preview image file before upload
 * @param {File} file - The image file to preview
 * @returns {Promise<string>} - Data URL for preview
 */
export const previewImage = (file) => {
  return new Promise((resolve, reject) => {
    if (!file) {
      reject(new Error('No file provided'));
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => resolve(e.target.result);
    reader.onerror = (error) => reject(error);
    reader.readAsDataURL(file);
  });
};
