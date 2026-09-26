var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var HttpExceptionFilter_1;
import { Catch, HttpException, HttpStatus, Logger, } from '@nestjs/common';
let HttpExceptionFilter = HttpExceptionFilter_1 = class HttpExceptionFilter {
    logger = new Logger(HttpExceptionFilter_1.name);
    catch(exception, host) {
        const ctx = host.switchToHttp();
        const response = ctx.getResponse();
        const request = ctx.getRequest();
        let status = HttpStatus.INTERNAL_SERVER_ERROR;
        let message = 'Lỗi hệ thống nội bộ';
        let error = 'Internal Server Error';
        if (exception instanceof HttpException) {
            status = exception.getStatus();
            const exceptionResponse = exception.getResponse();
            if (typeof exceptionResponse === 'string') {
                message = exceptionResponse;
                error = exception.name;
            }
            else if (typeof exceptionResponse === 'object' && exceptionResponse !== null) {
                const resObj = exceptionResponse;
                message = resObj.message || exception.message;
                error = resObj.error || exception.name;
            }
        }
        else if (exception instanceof Error) {
            message = exception.message;
            error = exception.name;
            this.logger.error(`Unhandled Exception: ${exception.message}`, exception.stack);
        }
        response.status(status).json({
            success: false,
            statusCode: status,
            message,
            error,
            path: request.url,
            timestamp: new Date().toISOString(),
        });
    }
};
HttpExceptionFilter = HttpExceptionFilter_1 = __decorate([
    Catch()
], HttpExceptionFilter);
export { HttpExceptionFilter };
//# sourceMappingURL=http-exception.filter.js.map