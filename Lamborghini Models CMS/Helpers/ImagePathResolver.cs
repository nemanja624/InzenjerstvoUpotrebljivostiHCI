using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Mercedes_Models_CMS.Helpers
{
    public static class ImagePathResolver
    {
        private const string ImagesFolderRelativePath = "../../../Images";

        public static string GetImagesDirectoryAbsolutePath()
        {
            return Path.GetFullPath(ImagesFolderRelativePath);
        }

        public static string? ResolveForDisplay(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            {
                return Path.GetFullPath(imagePath);
            }

            string directRelativeAbsolutePath = Path.GetFullPath(imagePath);
            if (File.Exists(directRelativeAbsolutePath))
            {
                return directRelativeAbsolutePath;
            }

            string fileName = Path.GetFileName(imagePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return imagePath;
            }

            string relativeCandidate = Path.Combine(ImagesFolderRelativePath, fileName);
            string absoluteCandidate = Path.GetFullPath(relativeCandidate);

            return File.Exists(absoluteCandidate) ? absoluteCandidate : imagePath;
        }

        public static string? NormalizeForStorage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            string? displayPath = ResolveForDisplay(imagePath);
            if (string.IsNullOrWhiteSpace(displayPath))
            {
                return imagePath;
            }

            string fileName = Path.GetFileName(displayPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return imagePath;
            }

            return Path.Combine(ImagesFolderRelativePath, fileName);
        }

        public static string SaveImageToProject(string sourceImagePath, string modelName)
        {
            string imagesDirectory = GetImagesDirectoryAbsolutePath();
            Directory.CreateDirectory(imagesDirectory);

            string extension = Path.GetExtension(sourceImagePath);
            string baseName = modelName;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalid, '_');
            }

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "model";
            }

            string fileName = $"{baseName}-{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
            string destinationPath = Path.Combine(imagesDirectory, fileName);
            File.Copy(sourceImagePath, destinationPath, true);

            return Path.Combine(ImagesFolderRelativePath, fileName);
        }

        public static BitmapImage? LoadBitmapForDisplay(string? imagePath)
        {
            string? resolvedPath = ResolveForDisplay(imagePath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            using FileStream stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
    }
}
