var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Injectable, } from '@nestjs/common';
import { map } from 'rxjs/operators';
let TransformInterceptor = class TransformInterceptor {
    intercept(context, next) {
        const http = context.switchToHttp();
        const response = http.getResponse();
        const statusCode = response.statusCode;
        return next.handle().pipe(map((res) => {
            if (res &&
                typeof res === 'object' &&
                'success' in res &&
                'data' in res) {
                return {
                    ...res,
                    statusCode: res.statusCode || statusCode,
                    timestamp: res.timestamp || new Date().toISOString(),
                };
            }
            let message = 'Thành công';
            let data = res;
            if (res && typeof res === 'object' && 'message' in res && 'data' in res) {
                message = res.message;
                data = res.data;
            }
            return {
                success: true,
                statusCode,
                message,
                data,
                timestamp: new Date().toISOString(),
            };
        }));
    }
};
TransformInterceptor = __decorate([
    Injectable()
], TransformInterceptor);
export { TransformInterceptor };
//# sourceMappingURL=transform.interceptor.js.map