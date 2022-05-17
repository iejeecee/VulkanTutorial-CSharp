using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanTest.Exceptions
{
    class ResultException 
    {
        public static void Throw(Result result, string message)
        {
            switch (result)
            {
                case Result.ErrorOutOfHostMemory: throw new OutOfHostMemoryError(message);
                case Result.ErrorOutOfDeviceMemory: throw new OutOfDeviceMemoryError(message);
                case Result.ErrorInitializationFailed: throw new InitializationFailedError(message);
                case Result.ErrorDeviceLost: throw new DeviceLostError(message);
                case Result.ErrorMemoryMapFailed: throw new MemoryMapFailedError(message);
                case Result.ErrorLayerNotPresent: throw new LayerNotPresentError(message);
                case Result.ErrorExtensionNotPresent: throw new ExtensionNotPresentError(message);
                case Result.ErrorFeatureNotPresent: throw new FeatureNotPresentError(message);
                case Result.ErrorIncompatibleDriver: throw new IncompatibleDriverError(message);
                case Result.ErrorTooManyObjects: throw new TooManyObjectsError(message);
                case Result.ErrorFormatNotSupported: throw new FormatNotSupportedError(message);
                case Result.ErrorFragmentedPool: throw new FragmentedPoolError(message);
                case Result.ErrorUnknown: throw new UnknownError(message);
                case Result.ErrorOutOfPoolMemory: throw new OutOfPoolMemoryError(message);
                case Result.ErrorInvalidExternalHandle: throw new InvalidExternalHandleError(message);
                case Result.ErrorFragmentation: throw new FragmentationError(message);
                case Result.ErrorInvalidOpaqueCaptureAddress: throw new InvalidOpaqueCaptureAddressError(message);
                case Result.ErrorSurfaceLostKhr: throw new SurfaceLostKhrError(message);
                case Result.ErrorNativeWindowInUseKhr: throw new NativeWindowInUseKhrError(message);
                case Result.ErrorOutOfDateKhr: throw new OutOfDateKhrError(message);
                case Result.ErrorIncompatibleDisplayKhr: throw new IncompatibleDisplayKhrError(message);
                case Result.ErrorValidationFailedExt: throw new ValidationFailedExtError(message);
                case Result.ErrorInvalidShaderNV: throw new InvalidShaderNVError(message);
                case Result.ErrorInvalidDrmFormatModifierPlaneLayoutExt: throw new InvalidDrmFormatModifierPlaneLayoutExtError(message);
                case Result.ErrorNotPermittedKhr: throw new NotPermittedKhrError(message);
#if VK_USE_PLATFORM_WIN32_KHR
        case Result.ErrorFullScreenExclusiveModeLostExt: throw new FullScreenExclusiveModeLostExtError( message );
#endif 
                default: throw new SystemError(message);
            }
        }
    }
}
